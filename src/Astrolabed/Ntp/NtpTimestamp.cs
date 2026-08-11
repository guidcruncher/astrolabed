using System;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace Astrolabed.Ntp;


public static class NtpTimestamp
{
    private static readonly DateTime NtpEpoch = new(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private const long TicksPerSecond = TimeSpan.TicksPerSecond;

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

        var ticks = (utc - NtpEpoch).Ticks;
        if (ticks < 0)
        {
            ticks = 0;
        }

        var seconds = (uint)(ticks / TicksPerSecond);
        var remainingTicks = ticks % TicksPerSecond;

        var fraction = (uint)((remainingTicks << 32) / TicksPerSecond);

        BinaryPrimitives.WriteUInt32BigEndian(destination[..4], seconds);
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

        long ticks = (seconds * TicksPerSecond) + ((fraction * TicksPerSecond) >> 32);
        return NtpEpoch.AddTicks(ticks);
    }
}
