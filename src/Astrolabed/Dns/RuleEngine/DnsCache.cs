using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;

namespace Astrolabed.Dns.RuleEngine;

public sealed class DnsCache
{
    private readonly ConcurrentDictionary<DnsCacheKey, CacheEntry> _entries = new();

    public bool TryGet(string domain, ushort type, ushort transactionId, out byte[]? response)
    {
        response = null;
        var key = new DnsCacheKey(domain, type);

        if (_entries.TryGetValue(key, out var entry))
        {
            if (DateTime.UtcNow < entry.Expires)
            {
                var copy = GC.AllocateUninitializedArray<byte>(entry.Length);
                Buffer.BlockCopy(entry.Buffer, 0, copy, 0, entry.Length);

                // Rewrite DNS Transaction ID (first 2 bytes) to match current query
                BinaryPrimitives.WriteUInt16BigEndian(copy.AsSpan(0, 2), transactionId);

                response = copy;
                return true;
            }

            // Entry expired — remove and return pooled buffer
            if (_entries.TryRemove(key, out var removed))
            {
                ArrayPool<byte>.Shared.Return(removed.Buffer, clearArray: true);
            }
        }

        return false;
    }

    public void Store(string domain, ushort type, byte[] response, TimeSpan ttl)
    {
        if (response == null || response.Length < 12 || ttl <= TimeSpan.Zero)
        {
            return;
        }

        var key = new DnsCacheKey(domain, type);
        var expires = DateTime.UtcNow + ttl;
        var buf = ArrayPool<byte>.Shared.Rent(response.Length);
        Buffer.BlockCopy(response, 0, buf, 0, response.Length);

        var newEntry = new CacheEntry(buf, response.Length, expires);

        _entries.AddOrUpdate(key,
            newEntry,
            (_, existing) =>
            {
                ArrayPool<byte>.Shared.Return(existing.Buffer, clearArray: true);
                return newEntry;
            });
    }

    public readonly record struct DnsCacheKey
    {
        public string Domain { get; }
        public ushort Type { get; }

        public DnsCacheKey(string domain, ushort type)
        {
            Domain = domain?.ToLowerInvariant() ?? string.Empty;
            Type = type;
        }
    }

    private sealed class CacheEntry
    {
        public byte[] Buffer { get; }
        public int Length { get; }
        public DateTime Expires { get; }

        public CacheEntry(byte[] buffer, int length, DateTime expires)
        {
            Buffer = buffer;
            Length = length;
            Expires = expires;
        }
    }
}
