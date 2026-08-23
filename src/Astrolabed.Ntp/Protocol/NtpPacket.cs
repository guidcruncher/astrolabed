using System.Buffers.Binary;

namespace Astrolabed.Ntp.Protocol;

/// <summary>
/// Represents an RFC 5905 compliant Network Time Protocol (NTP) binary packet structure.
/// </summary>
public sealed class NtpPacket
{
    /// <summary>
    /// Gets the standard fixed header size in bytes for an NTP packet without extension fields (48 bytes).
    /// </summary>
    public const int HeaderSize = 48;

    /// <summary>
    /// Gets or sets the 2-bit Leap Indicator.
    /// </summary>
    public NtpLeapIndicator LeapIndicator { get; set; } = NtpLeapIndicator.NoWarning;

    /// <summary>
    /// Gets or sets the 3-bit NTP Version Number (Default is 4).
    /// </summary>
    public byte VersionNumber { get; set; } = 4;

    /// <summary>
    /// Gets or sets the 3-bit NTP Mode (Default is Server).
    /// </summary>
    public NtpMode Mode { get; set; } = NtpMode.Server;

    /// <summary>
    /// Gets or sets the 8-bit Stratum level.
    /// </summary>
    public byte Stratum { get; set; } = 1;

    /// <summary>
    /// Gets or sets the 8-bit signed exponent representing the maximum interval between successive messages.
    /// </summary>
    public sbyte Poll { get; set; } = 4;

    /// <summary>
    /// Gets or sets the 8-bit signed exponent representing the precision of the system clock.
    /// </summary>
    public sbyte Precision { get; set; } = -20;

    /// <summary>
    /// Gets or sets the 32-bit total round-trip delay to the reference clock.
    /// </summary>
    public uint RootDelay { get; set; }

    /// <summary>
    /// Gets or sets the 32-bit total dispersion to the reference clock.
    /// </summary>
    public uint RootDispersion { get; set; }

    /// <summary>
    /// Gets or sets the 32-bit code identifying the particular reference clock.
    /// </summary>
    public uint ReferenceIdentifier { get; set; }

    /// <summary>
    /// Gets or sets the time when the system clock was last set or corrected.
    /// </summary>
    public NtpTimestamp ReferenceTimestamp { get; set; } = NtpTimestamp.Zero;

    /// <summary>
    /// Gets or sets the time at the client when the request departed for the server.
    /// </summary>
    public NtpTimestamp OriginTimestamp { get; set; } = NtpTimestamp.Zero;

    /// <summary>
    /// Gets or sets the time at the server when the request arrived from the client.
    /// </summary>
    public NtpTimestamp ReceiveTimestamp { get; set; } = NtpTimestamp.Zero;

    /// <summary>
    /// Gets or sets the time at the server when the response departed for the client.
    /// </summary>
    public NtpTimestamp TransmitTimestamp { get; set; } = NtpTimestamp.Zero;

    /// <summary>
    /// Gets or sets optional raw extension fields appended after the fixed 48-byte header.
    /// </summary>
    public ReadOnlyMemory<byte> ExtensionFields { get; set; } = ReadOnlyMemory<byte>.Empty;

    /// <summary>
    /// Serializes this <see cref="NtpPacket"/> instance into a destination byte span.
    /// </summary>
    /// <param name="destination">The destination target buffer span (must be at least 48 bytes).</param>
    /// <returns>The total number of bytes written to <paramref name="destination"/>.</returns>
    public int EncodeToSpan(Span<byte> destination)
    {
        int requiredSize = HeaderSize + ExtensionFields.Length;
        if (destination.Length < requiredSize)
        {
            throw new ArgumentException(
                $"Destination buffer size ({destination.Length} bytes) is insufficient. Minimum required: {requiredSize} bytes.",
                nameof(destination));
        }

        destination.Clear();

        // Byte 0: Bitwise packing LI (2 bits) | Version (3 bits) | Mode (3 bits)
        destination[0] = (byte)(((byte)LeapIndicator << 6) | ((VersionNumber & 0x07) << 3) | ((byte)Mode & 0x07));
        destination[1] = Stratum;
        destination[2] = (byte)Poll;
        destination[3] = (byte)Precision;

        BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(4, 4), RootDelay);
        BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(8, 4), RootDispersion);
        BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(12, 4), ReferenceIdentifier);

        ReferenceTimestamp.WriteToSpan(destination.Slice(16, 8));
        OriginTimestamp.WriteToSpan(destination.Slice(24, 8));
        ReceiveTimestamp.WriteToSpan(destination.Slice(32, 8));
        TransmitTimestamp.WriteToSpan(destination.Slice(40, 8));

        if (!ExtensionFields.IsEmpty)
        {
            ExtensionFields.Span.CopyTo(destination[HeaderSize..]);
        }

        return requiredSize;
    }

    /// <summary>
    /// Attempts to parse an <see cref="NtpPacket"/> from an incoming raw byte span.
    /// </summary>
    /// <param name="source">The source byte span containing binary packet payload.</param>
    /// <param name="packet">When successful, receives the parsed <see cref="NtpPacket"/> instance.</param>
    /// <returns><see langword="true"/> if decoding succeeded; otherwise <see langword="false"/>.</returns>
    public static bool TryDecode(ReadOnlySpan<byte> source, out NtpPacket? packet)
    {
        packet = null;

        if (source.Length < HeaderSize)
        {
            return false;
        }

        byte firstByte = source[0];

        packet = new NtpPacket
        {
            LeapIndicator = (NtpLeapIndicator)((firstByte >> 6) & 0x03),
            VersionNumber = (byte)((firstByte >> 3) & 0x07),
            Mode = (NtpMode)(firstByte & 0x07),
            Stratum = source[1],
            Poll = (sbyte)source[2],
            Precision = (sbyte)source[3],
            RootDelay = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(4, 4)),
            RootDispersion = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(8, 4)),
            ReferenceIdentifier = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(12, 4)),
            ReferenceTimestamp = NtpTimestamp.ReadFromSpan(source.Slice(16, 8)),
            OriginTimestamp = NtpTimestamp.ReadFromSpan(source.Slice(24, 8)),
            ReceiveTimestamp = NtpTimestamp.ReadFromSpan(source.Slice(32, 8)),
            TransmitTimestamp = NtpTimestamp.ReadFromSpan(source.Slice(40, 8))
        };

        if (source.Length > HeaderSize)
        {
            packet.ExtensionFields = source[HeaderSize..].ToArray();
        }

        return true;
    }
}
