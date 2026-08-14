using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Astrolabed.Dns.Core;

namespace Astrolabed.Dns.RuleEngine;

public sealed class DnsCache : IDisposable
{

    public Guid InstanceId { get; } = Guid.NewGuid();

    private readonly ConcurrentDictionary<DnsCacheKey, CacheEntry> _entries = new();
    private readonly int _maxCapacity;
    private readonly Timer _cleanupTimer;
    private readonly Channel<byte> _evictionSignal = Channel.CreateBounded<byte>(new BoundedChannelOptions(1)
    {
        FullMode = BoundedChannelFullMode.DropOldest
    });
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;

    public DnsCache(int maxCapacity = 10000, TimeSpan? cleanupInterval = null)
    {
        _maxCapacity = maxCapacity;
        var interval = cleanupInterval ?? TimeSpan.FromMinutes(1);
        _cleanupTimer = new Timer(_ => TriggerEviction(), null, interval, interval);

        Task.Factory.StartNew(
            () => ProcessEvictionsAsync(_cts.Token),
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    public bool TryGet(in DnsRequestContext context, out byte[]? response)
    {
        response = null;
        var key = new DnsCacheKey(context.Domain, context.QType, context.QClass);

        if (_entries.TryGetValue(key, out var entry))
        {
            if (DateTime.UtcNow < entry.Expires)
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
            return;
        }

        if (_entries.Count >= _maxCapacity)
        {
            TriggerEviction();
        }

        var key = new DnsCacheKey(context.Domain, context.QType, context.QClass);
        var expires = DateTime.UtcNow + ttl;

        var buf = ArrayPool<byte>.Shared.Rent(response.Length);
        CacheEntry? newEntry = null;

        try
        {
            Buffer.BlockCopy(response, 0, buf, 0, response.Length);
            newEntry = new CacheEntry(buf, response.Length, expires);

            _entries.AddOrUpdate(key,
                newEntry,
                (_, existing) =>
                {
                    existing.Dispose();
                    return newEntry;
                });
        }
        catch
        {
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
            catch
            {
                // Suppress background maintenance errors
            }
        }
    }

    private void PerformSweep(CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // Pass 1: Remove expired
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

        // Pass 2: Over capacity reduction
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

    public readonly record struct DnsCacheKey
    {
        public string Domain { get; }
        public ushort Type { get; }
        public ushort Class { get; }

        public DnsCacheKey(string domain, ushort type, ushort classCode)
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
            var buf = Interlocked.Exchange(ref _buffer, null);
            if (buf != null)
            {
                ArrayPool<byte>.Shared.Return(buf, clearArray: true);
            }
        }
    }
}
