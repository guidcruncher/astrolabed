using System.Buffers.Binary;
using System.Text;

namespace Astrolabed.Ntp.Protocol;

/// <summary>
/// High-performance binary serializer and deserializer for RFC 5905 Network Time Protocol (NTP) packets.
/// </summary>
public static class NtpPacketSerializer
{
    /// <summary>
    /// The standard fixed header size in bytes for an NTP packet without extension fields (48 bytes).
    /// </summary>
    public const int HeaderSize = 48;

    /// <summary>
    /// Deserializes a binary buffer span into an <see cref="NtpPacket"/> structure.
    /// </summary>
    /// <param name="buffer">The binary buffer span containing NTP packet payload.</param>
    /// <returns>The deserialized <see cref="NtpPacket"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="buffer"/> length is less than 48 bytes.</exception>
    public static NtpPacket Deserialize(ReadOnlySpan<byte> buffer)
    {
        if (!TryDeserialize(buffer, out NtpPacket? packet) || packet is null)
        {
            throw new ArgumentException($"NTP packet payload must be at least {HeaderSize} bytes long.", nameof(buffer));
        }

        return packet;
    }

    /// <summary>
    /// Attempts to deserialize a binary buffer span into an <see cref="NtpPacket"/> structure without throwing exceptions.
    /// </summary>
    /// <param name="buffer">The binary buffer span containing NTP packet payload.</param>
    /// <param name="packet">When successful, receives the deserialized <see cref="NtpPacket"/>.</param>
    /// <returns><see langword="true"/> if deserialization succeeded; otherwise <see langword="false"/>.</returns>
    public static bool TryDeserialize(ReadOnlySpan<byte> buffer, out NtpPacket? packet)
    {
        packet = null;

        if (buffer.Length < HeaderSize)
        {
            return false;
        }

        byte firstByte = buffer[0];
        var leapIndicator = (NtpLeapIndicator)((firstByte >> 6) & 0x03);
        byte versionNumber = (byte)((firstByte >> 3) & 0x07);
        var mode = (NtpMode)(firstByte & 0x07);

        byte stratum = buffer[1];
        sbyte poll = (sbyte)buffer[2];
        sbyte precision = (sbyte)buffer[3];

        uint rootDelay = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(4, 4));
        uint rootDispersion = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(8, 4));
        uint referenceIdentifier = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(12, 4));

        NtpTimestamp referenceTimestamp = ReadTimestamp(buffer.Slice(16, 8));
        NtpTimestamp originTimestamp = ReadTimestamp(buffer.Slice(24, 8));
        NtpTimestamp receiveTimestamp = ReadTimestamp(buffer.Slice(32, 8));
        NtpTimestamp transmitTimestamp = ReadTimestamp(buffer.Slice(40, 8));

        ReadOnlyMemory<byte> extensionFields = ReadOnlyMemory<byte>.Empty;
        if (buffer.Length > HeaderSize)
        {
            extensionFields = buffer[HeaderSize..].ToArray();
        }

        packet = new NtpPacket
        {
            LeapIndicator = leapIndicator,
            VersionNumber = versionNumber,
            Mode = mode,
            Stratum = stratum,
            Poll = poll,
            Precision = precision,
            RootDelay = rootDelay,
            RootDispersion = rootDispersion,
            ReferenceIdentifier = referenceIdentifier,
            ReferenceTimestamp = referenceTimestamp,
            OriginTimestamp = originTimestamp,
            ReceiveTimestamp = receiveTimestamp,
            TransmitTimestamp = transmitTimestamp,
            ExtensionFields = extensionFields
        };

        return true;
    }

    /// <summary>
    /// Serializes an <see cref="NtpPacket"/> instance into a destination byte span.
    /// </summary>
    /// <param name="packet">The NTP packet instance to serialize.</param>
    /// <param name="destination">The target destination byte span.</param>
    /// <returns>The total number of bytes written to <paramref name="destination"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="packet"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="destination"/> is too small.</exception>
    public static int Serialize(NtpPacket packet, Span<byte> destination)
    {
        ArgumentNullException.ThrowIfNull(packet);

        if (!TrySerialize(packet, destination, out int bytesWritten))
        {
            throw new ArgumentException("Destination buffer is too small for NTP packet serialization.", nameof(destination));
        }

        return bytesWritten;
    }

    /// <summary>
    /// Attempts to serialize an <see cref="NtpPacket"/> instance into a destination byte span.
    /// </summary>
    /// <param name="packet">The NTP packet instance to serialize.</param>
    /// <param name="destination">The target destination byte span.</param>
    /// <param name="bytesWritten">Receives the total number of bytes written to <paramref name="destination"/>.</param>
    /// <returns><see langword="true"/> if serialization succeeded; otherwise <see langword="false"/>.</returns>
    public static bool TrySerialize(NtpPacket packet, Span<byte> destination, out int bytesWritten)
    {
        bytesWritten = 0;

        if (packet is null)
        {
            return false;
        }

        int requiredSize = HeaderSize + packet.ExtensionFields.Length;
        if (destination.Length < requiredSize)
        {
            return false;
        }

        byte firstByte = (byte)(((byte)packet.LeapIndicator << 6) | ((packet.VersionNumber & 0x07) << 3) | ((byte)packet.Mode & 0x07));
        destination[0] = firstByte;
        destination[1] = packet.Stratum;
        destination[2] = (byte)packet.Poll;
        destination[3] = (byte)packet.Precision;

        BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(4, 4), packet.RootDelay);
        BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(8, 4), packet.RootDispersion);
        BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(12, 4), packet.ReferenceIdentifier);

        WriteTimestamp(destination.Slice(16, 8), packet.ReferenceTimestamp);
        WriteTimestamp(destination.Slice(24, 8), packet.OriginTimestamp);
        WriteTimestamp(destination.Slice(32, 8), packet.ReceiveTimestamp);
        WriteTimestamp(destination.Slice(40, 8), packet.TransmitTimestamp);

        if (!packet.ExtensionFields.IsEmpty)
        {
            packet.ExtensionFields.Span.CopyTo(destination[HeaderSize..]);
        }

        bytesWritten = requiredSize;
        return true;
    }

    /// <summary>
    /// Converts a 4-character ASCII reference identifier string (e.g. "GPS ", "LOCL") into a 32-bit unsigned integer without allocating heap memory.
    /// </summary>
    /// <param name="identifier">The reference identifier string.</param>
    /// <returns>The 32-bit big-endian integer representation.</returns>
    public static uint ConvertReferenceIdToUint(ReadOnlySpan<char> identifier)
    {
        Span<byte> bytes = stackalloc byte[4];
        bytes.Clear();

        int charsToProcess = Math.Min(identifier.Length, 4);
        Encoding.ASCII.GetBytes(identifier[..charsToProcess], bytes);

        return BinaryPrimitives.ReadUInt32BigEndian(bytes);
    }

    private static NtpTimestamp ReadTimestamp(ReadOnlySpan<byte> span)
    {
        uint seconds = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(0, 4));
        uint fraction = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(4, 4));
        return new NtpTimestamp(seconds, fraction);
    }

    private static void WriteTimestamp(Span<byte> span, NtpTimestamp timestamp)
    {
        BinaryPrimitives.WriteUInt32BigEndian(span.Slice(0, 4), timestamp.Seconds);
        BinaryPrimitives.WriteUInt32BigEndian(span.Slice(4, 4), timestamp.Fraction);
    }
}
