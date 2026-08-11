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

    public bool TryGet(string domain, byte[] request, ushort type, ushort classCode, out byte[]? response)
    {
        response = null;
        var key = new DnsCacheKey(domain, type, classCode);

        if (_entries.TryGetValue(key, out var entry))
        {
            if (DateTime.UtcNow < entry.Expires)
            {
                var copy = GC.AllocateUninitializedArray<byte>(entry.Length);

                if (!entry.TryCopyTo(copy))
                {
                    return false; // Entry was disposed concurrently
                }

                // 1. Patch DNS Transaction ID (Bytes 0-1)
                copy[0] = request[0];
                copy[1] = request[1];

                // 2. Patch RD (Recursion Desired) bit (Byte 2, Bit 0)
                // Preserves all other cached flags (QR, Opcode, AA, TC) but matches client's RD preference
                copy[2] = (byte)((copy[2] & 0xFE) | (request[2] & 0x01));

                // 3. Patch Question section for 0x20 case matching
                int qLen = GetQuestionLength(request);
                if (qLen > 0 && 12 + qLen <= copy.Length)
                {
                    Buffer.BlockCopy(request, 12, copy, 12, qLen);
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
                existing.Dispose(); // Thread-safe disposal of old buffer
                return newEntry;
            });
    }

    private void EnsureCapacity()
    {
        if (_entries.Count < _maxCapacity) return;

        // Pass 1: Remove all expired items
        SweepExpiredEntries(null);

        // Pass 2: If still at capacity, drop oldest items
        if (_entries.Count >= _maxCapacity)
        {
            int toRemove = _entries.Count - _maxCapacity + 1;
            foreach (var kvp in _entries)
            {
                if (toRemove <= 0) break;

                if (_entries.TryRemove(kvp.Key, out var removed))
                {
                    removed.Dispose();
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
                    removed.Dispose();
                }
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cleanupTimer.Dispose();

        foreach (var kvp in _entries)
        {
            if (_entries.TryRemove(kvp.Key, out var removed))
            {
                removed.Dispose();
            }
        }
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
                offset += 5; // +1 for 0-byte, +4 for QTYPE & QCLASS
                break;
            }
            if (len >= 192) // Compression pointer
            {
                offset += 6; // +2 for pointer, +4 for QTYPE & QCLASS
                break;
            }
            offset += len + 1;
        }
        return offset <= buffer.Length ? offset - 12 : 0;
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

    private sealed class CacheEntry : IDisposable
    {
        private byte[]? _buffer;
        public int Length { get; }
        public DateTime Expires { get; }

        public CacheEntry(byte[] buffer, int length, DateTime expires)
        {
            _buffer = buffer;
            Length = length;
            Expires = expires;
        }

        public bool TryCopyTo(byte[] destination)
        {
            var buf = Volatile.Read(ref _buffer);
            if (buf == null) return false;
            Buffer.BlockCopy(buf, 0, destination, 0, Length);
            return true;
        }

        public void Dispose()
        {
            // Ensures the rented buffer is returned exactly once, preventing leaks/double-returns during race conditions
            var buf = Interlocked.Exchange(ref _buffer, null);
            if (buf != null)
            {
                ArrayPool<byte>.Shared.Return(buf, clearArray: true);
            }
        }
    }
}
