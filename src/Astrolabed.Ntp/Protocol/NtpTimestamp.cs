using System.Buffers.Binary;

namespace Astrolabed.Ntp.Protocol;

/// <summary>
/// Represents a 64-bit Network Time Protocol (NTP) timestamp consisting of a 32-bit unsigned seconds count 
/// since January 1, 1900 00:00:00 UTC and a 32-bit fractional second component.
/// </summary>
/// <param name="Seconds">The 32-bit count of seconds since the NTP Epoch.</param>
/// <param name="Fraction">The 32-bit fractional second value in $2^{-32}$ second units.</param>
public readonly record struct NtpTimestamp(uint Seconds, uint Fraction)
{
    private const ulong FastFractionScale = 4_294_967_296UL; // 2^32
    private static readonly DateTimeOffset NtpEpoch = new(1900, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Gets an empty NTP timestamp instance representing zero seconds and zero fraction.
    /// </summary>
    public static NtpTimestamp Zero => new(0, 0);

    /// <summary>
    /// Creates an <see cref="NtpTimestamp"/> representing the current UTC system time.
    /// </summary>
    public static NtpTimestamp UtcNow => FromDateTimeOffset(DateTimeOffset.UtcNow);

    /// <summary>
    /// Converts a <see cref="DateTimeOffset"/> instance into an equivalent <see cref="NtpTimestamp"/>.
    /// </summary>
    /// <param name="dateTime">The target date time offset to convert.</param>
    /// <returns>An <see cref="NtpTimestamp"/> representing the UTC time.</returns>
    public static NtpTimestamp FromDateTimeOffset(DateTimeOffset dateTime)
    {
        DateTimeOffset utcTime = dateTime.ToUniversalTime();
        if (utcTime < NtpEpoch)
        {
            return Zero;
        }

        TimeSpan duration = utcTime - NtpEpoch;
        ulong totalSeconds = (ulong)duration.Ticks / (ulong)TimeSpan.TicksPerSecond;
        uint seconds = (uint)(totalSeconds & 0xFFFFFFFF);

        ulong remainingTicks = (ulong)(duration.Ticks % TimeSpan.TicksPerSecond);
        uint fraction = (uint)((remainingTicks * FastFractionScale) / (ulong)TimeSpan.TicksPerSecond);

        return new NtpTimestamp(seconds, fraction);
    }

    /// <summary>
    /// Converts this <see cref="NtpTimestamp"/> instance into an equivalent <see cref="DateTimeOffset"/> in UTC.
    /// </summary>
    /// <returns>The calculated <see cref="DateTimeOffset"/>.</returns>
    public DateTimeOffset ToDateTimeOffset()
    {
        if (Seconds == 0 && Fraction == 0)
        {
            return NtpEpoch;
        }

        ulong durationTicks = ((ulong)Seconds * TimeSpan.TicksPerSecond) +
                              (((ulong)Fraction * TimeSpan.TicksPerSecond) / FastFractionScale);

        return NtpEpoch.AddTicks((long)durationTicks);
    }

    /// <summary>
    /// Reads a 64-bit big-endian NTP timestamp directly from a source byte span.
    /// </summary>
    /// <param name="source">The source buffer (must be at least 8 bytes long).</param>
    /// <returns>The decoded <see cref="NtpTimestamp"/>.</returns>
    public static NtpTimestamp ReadFromSpan(ReadOnlySpan<byte> source)
    {
        if (source.Length < 8)
        {
            throw new ArgumentException("Source buffer must be at least 8 bytes long.", nameof(source));
        }

        uint seconds = BinaryPrimitives.ReadUInt32BigEndian(source[..4]);
        uint fraction = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(4, 4));

        return new NtpTimestamp(seconds, fraction);
    }

    /// <summary>
    /// Writes this <see cref="NtpTimestamp"/> instance as a 64-bit big-endian binary payload into a target destination span.
    /// </summary>
    /// <param name="destination">The destination byte span (must be at least 8 bytes long).</param>
    public void WriteToSpan(Span<byte> destination)
    {
        if (destination.Length < 8)
        {
            throw new ArgumentException("Destination buffer must be at least 8 bytes long.", nameof(destination));
        }

        BinaryPrimitives.WriteUInt32BigEndian(destination[..4], Seconds);
        BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(4, 4), Fraction);
    }
}
