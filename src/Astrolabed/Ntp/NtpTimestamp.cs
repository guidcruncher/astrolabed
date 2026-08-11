using System;
using System.Buffers.Binary;

namespace Astrolabed.Ntp;

public static class NtpTimestamp
{
    private static readonly DateTime Epoch = new(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private const long TicksPerSecond = TimeSpan.TicksPerSecond; // 10,000,000

    public static void WriteTimestamp(Span<byte> destination, DateTime utc)
    {
        if (destination.Length < 8)
        {
            throw new ArgumentException("Destination span must be at least 8 bytes.", nameof(destination));
        }

        if (utc.Kind != DateTimeKind.Utc)
        {
            utc = utc.ToUniversalTime();
        }

        var ticks = (utc - Epoch).Ticks;

        if (ticks < 0)
        {
            ticks = 0;
        }

        var seconds = (uint)(ticks / TicksPerSecond);
        var remainingTicks = ticks % TicksPerSecond;

        // Scale 100-ns ticks (10,000,000 per sec) across 32-bit fraction range (2^32)
        var fraction = (uint)((remainingTicks << 32) / TicksPerSecond);

        BinaryPrimitives.WriteUInt32BigEndian(destination[..4], seconds);
        BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(4, 4), fraction);
    }

    public static void WriteTimestamp(byte[] buffer, int offset, DateTime utc)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        if (offset < 0 || offset + 8 > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Offset is outside the bounds of the buffer.");
        }

        WriteTimestamp(buffer.AsSpan(offset, 8), utc);
    }
}
