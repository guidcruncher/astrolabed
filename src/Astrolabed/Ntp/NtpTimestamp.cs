using System;
using System.Buffers.Binary;

namespace Astrolabed.Ntp;

public static class NtpTimestamp
{
    private static readonly DateTime Era0Epoch = new(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private const long TicksPerSecond = TimeSpan.TicksPerSecond;
    private const long SecondsPerEra = 0x100000000L; // 2^32 seconds (~136 years)

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

        var totalTicks = (utc - Era0Epoch).Ticks;
        if (totalTicks < 0)
        {
            totalTicks = 0;
        }

        var totalSeconds = totalTicks / TicksPerSecond;
        var eraSeconds = (uint)(totalSeconds % SecondsPerEra);
        var remainingTicks = totalTicks % TicksPerSecond;

        var fraction = (uint)((remainingTicks << 32) / TicksPerSecond);

        BinaryPrimitives.WriteUInt32BigEndian(destination[..4], eraSeconds);
        BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(4, 4), fraction);
    }

    public static void WriteTimestamp(byte[] buffer, int offset, DateTime utc)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        if (offset < 0 || offset + 8 > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Offset is outside buffer bounds.");
        }

        WriteTimestamp(buffer.AsSpan(offset, 8), utc);
    }

    public static DateTime ReadTimestamp(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < 8)
        {
            throw new ArgumentException("Buffer must be at least 8 bytes.", nameof(buffer));
        }

        uint seconds = BinaryPrimitives.ReadUInt32BigEndian(buffer[..4]);
        uint fraction = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(4, 4));

        long eraOffsetSeconds = 0;

        // If high bit is 0, timestamp is in Era 1 (post-Feb 2036).
        if ((seconds & 0x80000000) == 0)
        {
            eraOffsetSeconds = SecondsPerEra;
        }

        long totalSeconds = seconds + eraOffsetSeconds;
        long ticks = (totalSeconds * TicksPerSecond) + ((fraction * TicksPerSecond) >> 32);

        return Era0Epoch.AddTicks(ticks);
    }
}
