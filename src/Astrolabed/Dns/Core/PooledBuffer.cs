using System;
using System.Buffers;
using System.Threading;

namespace Astrolabed.Dns.Core;

/// <summary>
/// A wrapper for pooled byte array buffers that enforces thread-safe, idempotent return to ArrayPool.
/// </summary>
public sealed class PooledBuffer : IDisposable
{
    private int _returned;

    public byte[] Buffer { get; }
    public int Length { get; }
    public bool FromPool { get; }
    public bool ClearArray { get; }

    public PooledBuffer(byte[] buffer, int length, bool fromPool = true, bool clearArray = false)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, buffer.Length);

        Buffer = buffer;
        Length = length;
        FromPool = fromPool;
        ClearArray = clearArray;
    }

    public ReadOnlySpan<byte> Span => Buffer.AsSpan(0, Length);

    public ReadOnlyMemory<byte> Memory => Buffer.AsMemory(0, Length);

    public void Return()
    {
        if (FromPool && Interlocked.Exchange(ref _returned, 1) == 0)
        {
            ArrayPool<byte>.Shared.Return(Buffer, ClearArray);
        }
    }

    public void Dispose() => Return();
}
