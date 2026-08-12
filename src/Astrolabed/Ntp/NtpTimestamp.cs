using System;
using System.Buffers.Binary;

namespace Astrolabed.Ntp;

public static class NtpTimestamp
{
    private static readonly DateTime Era0Epoch = new(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private const long TicksPerSecond = TimeSpan.TicksPerSecond;
    private const long SecondsPerEra = 0x100000000L;
    private const double SecondsPerEraDouble = 4294967296.0;

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

        long totalTicks = (utc - Era0Epoch).Ticks;
        if (totalTicks < 0)
        {
            totalTicks = 0;
        }

        long totalSeconds = totalTicks / TicksPerSecond;
        uint eraSeconds = (uint)(totalSeconds % SecondsPerEra);
        long remainingTicks = totalTicks % TicksPerSecond;

        uint fraction = (uint)((remainingTicks << 32) / TicksPerSecond);

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

        long currentSystemSeconds = (DateTime.UtcNow - Era0Epoch).Ticks / TicksPerSecond;
        long currentEra = currentSystemSeconds / SecondsPerEra;
        long eraBaseSeconds = currentEra * SecondsPerEra;

        long candidateSeconds = eraBaseSeconds + seconds;

        if (candidateSeconds - currentSystemSeconds > (SecondsPerEra / 2))
        {
            candidateSeconds -= SecondsPerEra;
        }
        else if (currentSystemSeconds - candidateSeconds > (SecondsPerEra / 2))
        {
            candidateSeconds += SecondsPerEra;
        }

        long fractionTicks = (long)Math.Round(fraction * TicksPerSecond / SecondsPerEraDouble);
        long ticks = (candidateSeconds * TicksPerSecond) + fractionTicks;

        return Era0Epoch.AddTicks(ticks);
    }
}
