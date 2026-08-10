using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Threading;

namespace Astrolabed.Dns.RuleEngine;

public sealed class DnsCache : IDisposable
{
    private readonly ConcurrentDictionary<DnsCacheKey, CacheEntry> _entries = new();
    private readonly int _maxCapacity;
    private readonly Timer _cleanupTimer;
    private bool _disposed;

    public DnsCache(int maxCapacity = 10000, TimeSpan? cleanupInterval = null)
    {
        _maxCapacity = maxCapacity;
        var interval = cleanupInterval ?? TimeSpan.FromMinutes(1);
        _cleanupTimer = new Timer(SweepExpiredEntries, null, interval, interval);
    }

    public bool TryGet(string domain, ushort type, ushort transactionId, out byte[]? response, ushort classCode = 1)
    {
        response = null;
        var key = new DnsCacheKey(domain, type, classCode);

        if (_entries.TryGetValue(key, out var entry))
        {
            if (DateTime.UtcNow < entry.Expires)
            {
                var copy = GC.AllocateUninitializedArray<byte>(entry.Length);
                Buffer.BlockCopy(entry.Buffer, 0, copy, 0, entry.Length);

                // Patch DNS Transaction ID (first 2 bytes)
                BinaryPrimitives.WriteUInt16BigEndian(copy.AsSpan(0, 2), transactionId);

                response = copy;
                return true;
            }

            if (_entries.TryRemove(key, out var removed))
            {
                ArrayPool<byte>.Shared.Return(removed.Buffer, clearArray: true);
            }
        }

        return false;
    }

    public void Store(string domain, ushort type, byte[] response, TimeSpan ttl, ushort classCode = 1)
    {
        if (response == null || response.Length < 12 || ttl <= TimeSpan.Zero)
        {
            return;
        }

        EnsureCapacity();

        var key = new DnsCacheKey(domain, type, classCode);
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

    private void EnsureCapacity()
    {
        if (_entries.Count < _maxCapacity)
        {
            return;
        }

        // Pass 1: Remove all expired items
        SweepExpiredEntries(null);

        // Pass 2: If still at capacity, drop oldest items until room is made
        if (_entries.Count >= _maxCapacity)
        {
            int toRemove = _entries.Count - _maxCapacity + 1;
            foreach (var kvp in _entries)
            {
                if (toRemove <= 0)
                {
                    break;
                }

                if (_entries.TryRemove(kvp.Key, out var removed))
                {
                    ArrayPool<byte>.Shared.Return(removed.Buffer, clearArray: true);
                    toRemove--;
                }
            }
        }
    }

    private void SweepExpiredEntries(object? state)
    {
        var now = DateTime.UtcNow;
        foreach (var kvp in _entries)
        {
            if (kvp.Value.Expires <= now)
            {
                if (_entries.TryRemove(kvp.Key, out var removed))
                {
                    ArrayPool<byte>.Shared.Return(removed.Buffer, clearArray: true);
                }
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cleanupTimer.Dispose();

        foreach (var kvp in _entries)
        {
            if (_entries.TryRemove(kvp.Key, out var removed))
            {
                ArrayPool<byte>.Shared.Return(removed.Buffer, clearArray: true);
            }
        }
    }

    public readonly record struct DnsCacheKey
    {
        public string Domain { get; }
        public ushort Type { get; }
        public ushort Class { get; }

        public DnsCacheKey(string domain, ushort type, ushort classCode = 1)
        {
            Domain = domain?.ToLowerInvariant() ?? string.Empty;
            Type = type;
            Class = classCode;
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
