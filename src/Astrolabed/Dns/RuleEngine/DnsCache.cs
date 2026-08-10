using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Astrolabed.Dns.RuleEngine;

public sealed class DnsCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new(StringComparer.OrdinalIgnoreCase);

    public bool TryGet(string domain, out byte[]? response)
    {
        response = null;

        if (_entries.TryGetValue(domain, out var entry))
        {
            if (Environment.TickCount64 < entry.ExpiresTicks)
            {
                // Allocate uninitialized memory since BlockCopy will overwrite every byte
                var copy = GC.AllocateUninitializedArray<byte>(entry.Length);
                Buffer.BlockCopy(entry.Buffer, 0, copy, 0, entry.Length);
                response = copy;
                return true;
            }

            // Remove only if the entry hasn't been updated concurrently
            if (_entries.TryRemove(KeyValuePair.Create(domain, entry)))
            {
                ArrayPool<byte>.Shared.Return(entry.Buffer, clearArray: false);
            }
        }

        return false;
    }

    public void Store(string domain, byte[] response, TimeSpan ttl)
    {
        long expiresTicks = Environment.TickCount64 + (long)ttl.TotalMilliseconds;
        var pool = ArrayPool<byte>.Shared;
        var buf = pool.Rent(response.Length);
        Buffer.BlockCopy(response, 0, buf, 0, response.Length);

        var newEntry = new CacheEntry(buf, response.Length, expiresTicks);

        _entries.AddOrUpdate(
            domain,
            newEntry,
            (_, existing) =>
            {
                pool.Return(existing.Buffer, clearArray: false);
                return newEntry;
            });
    }

    // Try to return the pooled cached buffer directly (caller must not return it to the pool).
    public bool TryGetPooled(string domain, out byte[]? buffer, out int length)
    {
        buffer = null;
        length = 0;

        if (_entries.TryGetValue(domain, out var entry))
        {
            if (Environment.TickCount64 < entry.ExpiresTicks)
            {
                buffer = entry.Buffer;
                length = entry.Length;
                return true;
            }

            if (_entries.TryRemove(KeyValuePair.Create(domain, entry)))
            {
                ArrayPool<byte>.Shared.Return(entry.Buffer, clearArray: false);
            }
        }

        return false;
    }

    private readonly record struct CacheEntry(byte[] Buffer, int Length, long ExpiresTicks);
}
