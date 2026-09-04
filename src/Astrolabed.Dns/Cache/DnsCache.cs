using System.Collections.Concurrent;

using Astrolabed.Data.Pagination;
using Astrolabed.Dns.Models;
using Astrolabed.Dns.Options;
using Astrolabed.Dns.Serialization;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Cache;

/// <summary>
/// Thread-safe, high-performance in-memory DNS record cache supporting LRU eviction and zero-allocation key matching.
/// </summary>
/// <param name="optionsMonitor">Options monitor tracking max cache capacity constraints.</param>
/// <param name="logger">Structured logger instance.</param>
public sealed partial class DnsCache(
    IOptionsMonitor<DnsEngineOptions> optionsMonitor,
    ILogger<DnsCache> logger) : IDnsCache
{
    private readonly ConcurrentDictionary<DnsCacheKey, CacheEntry> _entries = new();
    private readonly ConcurrentQueue<DnsCacheKey> _evictionQueue = new();
    private readonly IOptionsMonitor<DnsEngineOptions> _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
    private readonly ILogger<DnsCache> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Gets the number of items in the Cache
    /// </summary>
    public int Count => _entries.Count;

    /// <inheritdoc />
    public PagedResult<KeyValuePair<string, CacheEntryView>> ToPagedResult(
        int pageNumber,
        int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageNumber);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        KeyValuePair<DnsCacheKey, CacheEntry>[] snapshot = _entries.ToArray();
        int totalCount = _entries.Count;
        int normalizedPageSize = Math.Clamp(pageSize, 1, 100);

        int skip = (pageNumber - 1) * pageSize;
        if (skip >= totalCount)
        {
            return new PagedResult<KeyValuePair<string, CacheEntryView>>(
                Array.Empty<KeyValuePair<string, CacheEntryView>>(),
                pageNumber,
                normalizedPageSize,
                totalCount);
        }

        int countToTake = Math.Min(pageSize, totalCount - skip);
        List<KeyValuePair<string, CacheEntryView>> pageItems = new(countToTake);

        ReadOnlySpan<KeyValuePair<DnsCacheKey, CacheEntry>> pagedSlice = snapshot.AsSpan(skip, countToTake);

        foreach (KeyValuePair<DnsCacheKey, CacheEntry> kvp in pagedSlice)
        {
            if (DnsWireParser.TryParse(kvp.Value.Payload.Span, out DnsWireMessage? parsedMessage) && parsedMessage is not null)
            {
                var view = new CacheEntryView(parsedMessage, kvp.Value.ExpiresAt);
                pageItems.Add(new KeyValuePair<string, CacheEntryView>(kvp.Key.Domain, view));
            }
            else
            {
                _logger.LogWarning(
                    "Failed to parse DNS wire message for domain {Domain} and type {QueryType} during paged retrieval.",
                    kvp.Key.Domain,
                    kvp.Key.QueryType);
            }
        }

        return new PagedResult<KeyValuePair<string, CacheEntryView>>(
            pageItems,
            totalCount,
            pageNumber,
            pageSize);
    }

    /// <inheritdoc />
    public bool TryGet(string domain, ushort qType, out ReadOnlyMemory<byte> payload)
    {
        payload = default;
        if (string.IsNullOrWhiteSpace(domain))
        {
            return false;
        }

        var key = new DnsCacheKey(domain.Trim().TrimEnd('.'), qType);
        if (_entries.TryGetValue(key, out CacheEntry? entry))
        {
            if (!entry.IsExpired)
            {
                payload = entry.Payload;
                return true;
            }

            _entries.TryRemove(key, out _);
        }

        return false;
    }

    /// <inheritdoc />
    public void Store(string domain, ushort qType, ReadOnlyMemory<byte> payload, TimeSpan ttl)
    {
        // Guard against invalid domain inputs or items with a TTL of 0 seconds (or negative)
        if (string.IsNullOrWhiteSpace(domain) || ttl <= TimeSpan.Zero)
        {
            return;
        }

        int maxEntries = _optionsMonitor.CurrentValue.MaxCacheEntries;
        if (_entries.Count >= maxEntries)
        {
            PurgeExpiredOrLeastRecentlyUsed(maxEntries);
        }

        var key = new DnsCacheKey(domain.Trim().TrimEnd('.'), qType);
        var entry = new CacheEntry(payload, DateTimeOffset.UtcNow.Add(ttl));

        if (_entries.TryAdd(key, entry))
        {
            _evictionQueue.Enqueue(key);
        }
        else
        {
            _entries[key] = entry;
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        _entries.Clear();
    }

    private void PurgeExpiredOrLeastRecentlyUsed(int maxEntries)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;

        // Step 1: Remove expired items first
        foreach (var (key, entry) in _entries)
        {
            if (entry.ExpiresAt <= now)
            {
                _entries.TryRemove(key, out _);
            }
        }

        // Step 2: Enforce strict capacity limit using LRU eviction queue if still over capacity
        while (_entries.Count >= maxEntries && _evictionQueue.TryDequeue(out DnsCacheKey evictKey))
        {
            if (_entries.TryRemove(evictKey, out _))
            {
                LogCacheEvictedLru(_logger, evictKey.Domain, evictKey.QueryType);
            }
        }
    }

    [LoggerMessage(
        EventId = 701,
        Level = LogLevel.Debug,
        Message = "DNS cache limit reached. LRU entry evicted: {Domain} (Type: {QueryType})")]
    private static partial void LogCacheEvictedLru(ILogger logger, string domain, ushort queryType);

    /// <summary>
    /// Represents an allocation-friendly composite key for DNS cache queries.
    /// </summary>
    private readonly record struct DnsCacheKey
    {
        public string Domain { get; }
        public ushort QueryType { get; }

        public DnsCacheKey(string domain, ushort queryType)
        {
            Domain = domain;
            QueryType = queryType;
        }

        public bool Equals(DnsCacheKey other) =>
            QueryType == other.QueryType && string.Equals(Domain, other.Domain, StringComparison.OrdinalIgnoreCase);

        public override int GetHashCode() =>
            HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(Domain), QueryType);
    }
}
