using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Astrolabed.Dns.Core;

public sealed class CachingDnsClientDecorator : IDnsClient
{
    private readonly IDnsClient _inner;
    private readonly ConcurrentDictionary<CacheKey, CacheEntry> _cache = new();
    private readonly int _maxEntries;
    private int _count;

    public CachingDnsClientDecorator(IDnsClient inner, int maxEntries)
    {
        _inner = inner;
        _maxEntries = maxEntries;
    }

    public async Task<byte[]> QueryAsync(byte[] request, CancellationToken ct)
    {
        var msg = DnsParser.Parse(request);
        if (msg.Questions.Count == 0)
        {
            return await _inner.QueryAsync(request, ct).ConfigureAwait(false);
        }

        var q = msg.Questions[0];
        var key = new CacheKey(q.Name, q.Type.GetHashCode());
        long now = Environment.TickCount64;

        if (_cache.TryGetValue(key, out var entry))
        {
            if (now < entry.ExpiresTicks)
            {
                return entry.Response;
            }

            // Lazy cleanup of expired entry
            if (_cache.TryRemove(key, out _))
            {
                Interlocked.Decrement(ref _count);
            }
        }

        var response = await _inner.QueryAsync(request, ct).ConfigureAwait(false);
        var respMsg = DnsParser.Parse(response);
        int ttl = respMsg.GetMinTtl();

        if (ttl > 0)
        {
            long expiresTicks = now + (ttl * 1000L);
            var newEntry = new CacheEntry(response, expiresTicks);

            if (_cache.ContainsKey(key))
            {
                _cache[key] = newEntry;
            }
            else if (Volatile.Read(ref _count) < _maxEntries)
            {
                if (_cache.TryAdd(key, newEntry))
                {
                    Interlocked.Increment(ref _count);
                }
            }
        }

        return response;
    }

    private readonly record struct CacheKey(string Name, int TypeHash);
    private readonly record struct CacheEntry(byte[] Response, long ExpiresTicks);
}
