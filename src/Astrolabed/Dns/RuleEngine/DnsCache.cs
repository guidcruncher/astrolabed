using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Astrolabed.Dns.Core;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.RuleEngine;

public sealed class DnsCache : IDnsCache, IDisposable
{
    public Guid InstanceId { get; } = Guid.NewGuid();

    private readonly Lock _sync = new();
    private readonly ILogger<DnsCache> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly int _maxCapacity;
    private readonly ITimer _cleanupTimer;
    private readonly Channel<byte> _evictionSignal;
    private readonly CancellationTokenSource _cts = new();

    private ConcurrentDictionary<DnsCacheKey, CacheEntry> _entries = new();
    private bool _disposed;

    public DnsCache(
        IOptions<CachingOptions> options,
        ILogger<DnsCache> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;

        var config = options.Value;
        _maxCapacity = config.MaxEntries > 0 ? config.MaxEntries : 10000;

        _evictionSignal = Channel.CreateBounded<byte>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

        _cleanupTimer = _timeProvider.CreateTimer(
            _ => TriggerEviction(),
            null,
            TimeSpan.FromMinutes(config.CleanupIntervalMinutes),
            TimeSpan.FromMinutes(config.CleanupIntervalMinutes));

        Task.Factory.StartNew(
            () => ProcessEvictionsAsync(_cts.Token),
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

        _logger.LogInformation("DNS Cache initialized with instance ID {InstanceId} and max capacity {MaxEntries}.", InstanceId, _maxCapacity);
    }

    public void Flush()
    {
        ConcurrentDictionary<DnsCacheKey, CacheEntry> oldEntries;

        lock (_sync)
        {
            oldEntries = _entries;
            _entries = new ConcurrentDictionary<DnsCacheKey, CacheEntry>();
        }

        int clearedCount = 0;
        foreach (var kvp in oldEntries)
        {
            if (oldEntries.TryRemove(kvp.Key, out var removed))
            {
                removed.Dispose();
                clearedCount++;
            }
        }

        _logger.LogInformation("Flushed {Count} DNS cache entries for instance {InstanceId}.", clearedCount, InstanceId);
    }

    public bool TryGet(in DnsRequestContext context, out byte[]? response)
    {
        response = null;
        var key = new DnsCacheKey(context.Domain, context.QType, context.QClass);

        if (_entries.TryGetValue(key, out var entry))
        {
            if (_timeProvider.GetUtcNow() < entry.Expires)
            {
                var copy = GC.AllocateUninitializedArray<byte>(entry.Length);

                if (!entry.TryCopyTo(copy))
                {
                    return false;
                }

                // 1. Patch DNS Transaction ID
                copy[0] = context.RawRequest[0];
                copy[1] = context.RawRequest[1];

                // 2. Patch RD flag
                copy[2] = (byte)((copy[2] & 0xFE) | (context.RawRequest[2] & 0x01));

                // 3. Patch Question section for 0x20 case matching
                int qLen = GetQuestionLength(context.RawRequest);
                if (qLen > 0 && 12 + qLen <= copy.Length)
                {
                    Buffer.BlockCopy(context.RawRequest, 12, copy, 12, qLen);
                }

                response = copy;
                return true;
            }

            if (_entries.TryRemove(key, out var removed))
            {
                removed.Dispose();
            }
        }

        return false;
    }

    public void Store(in DnsRequestContext context, byte[] response, TimeSpan ttl)
    {
        if (response == null || response.Length < 12 || ttl <= TimeSpan.Zero)
        {
            return;
        }

        // Water Torture Guard: Prevent caching single-use NXDOMAIN responses unless they carry a valid positive TTL
        int rcode = response[3] & 0x0F;
        if (rcode == 3 && ttl.TotalSeconds < 5)
        {
            _logger.LogDebug("Ignoring NXDOMAIN response for {Domain} due to low TTL.", context.Domain);
            return;
        }

        if (_entries.Count >= _maxCapacity)
        {
            TriggerEviction();
        }

        var key = new DnsCacheKey(context.Domain, context.QType, context.QClass);
        var expires = _timeProvider.GetUtcNow() + ttl;

        var buf = ArrayPool<byte>.Shared.Rent(response.Length);
        CacheEntry? newEntry = null;

        try
        {
            Buffer.BlockCopy(response, 0, buf, 0, response.Length);
            newEntry = new CacheEntry(buf, response.Length, expires);

            _entries.AddOrUpdate(
                key,
                newEntry,
                (_, existing) =>
                {
                    existing.Dispose();
                    return newEntry;
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while storing cache entry for {Domain}.", context.Domain);
            if (newEntry != null)
            {
                newEntry.Dispose();
            }
            else
            {
                ArrayPool<byte>.Shared.Return(buf, clearArray: true);
            }
            throw;
        }
    }

    private void TriggerEviction()
    {
        _evictionSignal.Writer.TryWrite(0);
    }

    private async Task ProcessEvictionsAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await _evictionSignal.Reader.ReadAsync(ct).ConfigureAwait(false);
                PerformSweep(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "An error occurred during the cache eviction processing loop.");
            }
        }
    }

    private void PerformSweep(CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow();

        // Pass 1: Remove expired entries
        foreach (var kvp in _entries)
        {
            if (ct.IsCancellationRequested) break;

            if (kvp.Value.Expires <= now)
            {
                if (_entries.TryRemove(kvp.Key, out var removed))
                {
                    removed.Dispose();
                }
            }
        }

        // Pass 2: Reduce over capacity
        if (!ct.IsCancellationRequested && _entries.Count >= _maxCapacity)
        {
            int toRemove = _entries.Count - _maxCapacity + 1;
            foreach (var kvp in _entries)
            {
                if (toRemove <= 0 || ct.IsCancellationRequested) break;

                if (_entries.TryRemove(kvp.Key, out var removed))
                {
                    removed.Dispose();
                    toRemove--;
                }
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts.Cancel();
        _cts.Dispose();
        _cleanupTimer.Dispose();

        foreach (var kvp in _entries)
        {
            if (_entries.TryRemove(kvp.Key, out var removed))
            {
                removed.Dispose();
            }
        }

        _logger.LogInformation("DNS Cache instance {InstanceId} disposed.", InstanceId);
    }

    private static int GetQuestionLength(byte[] buffer)
    {
        if (buffer.Length < 12) return 0;
        int offset = 12;
        while (offset < buffer.Length)
        {
            byte len = buffer[offset];
            if (len == 0)
            {
                offset += 5;
                break;
            }
            if (len >= 192)
            {
                offset += 6;
                break;
            }
            offset += len + 1;
        }
        return offset <= buffer.Length ? offset - 12 : 0;
    }

    public readonly record struct DnsCacheKey(string Domain, ushort Type, ushort Class)
    {
        public string Domain { get; } = Domain?.ToLowerInvariant() ?? string.Empty;
        public ushort Type { get; } = Type;
        public ushort Class { get; } = Class;
    }

    private sealed class CacheEntry(byte[] buffer, int length, DateTimeOffset expires) : IDisposable
    {
        private byte[]? _buffer = buffer;

        public int Length { get; } = length;
        public DateTimeOffset Expires { get; } = expires;

        public bool TryCopyTo(byte[] destination)
        {
            var buf = Volatile.Read(ref _buffer);
            if (buf == null) return false;
            Buffer.BlockCopy(buf, 0, destination, 0, Length);
            return true;
        }

        public void Dispose()
        {
            var buf = Interlocked.Exchange(ref _buffer, null);
            if (buf != null)
            {
                ArrayPool<byte>.Shared.Return(buf, clearArray: true);
            }
        }
    }
}
