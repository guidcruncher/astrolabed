using System.Buffers.Binary;
using System.Text;

namespace Astrolabed.Ntp.Protocol;

public static class NtpPacketSerializer
{
    public const int HeaderSize = 48;

    public static NtpPacket Deserialize(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < HeaderSize)
        {
            throw new ArgumentException($"NTP packet must be at least {HeaderSize} bytes long.", nameof(buffer));
        }

        byte firstByte = buffer[0];
        NtpLeapIndicator leapIndicator = (NtpLeapIndicator)((firstByte >> 6) & 0x03);
        byte versionNumber = (byte)((firstByte >> 3) & 0x07);
        NtpMode mode = (NtpMode)(firstByte & 0x07);

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

        byte[] extensionFields = Array.Empty<byte>();
        if (buffer.Length > HeaderSize)
        {
            extensionFields = buffer.Slice(HeaderSize).ToArray();
        }

        return new NtpPacket
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
    }

    public static int Serialize(NtpPacket packet, Span<byte> destination)
    {
        if (destination.Length < HeaderSize + packet.ExtensionFields.Length)
        {
            throw new ArgumentException("Destination buffer is too small for NTP packet serialization.", nameof(destination));
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

        if (packet.ExtensionFields.Length > 0)
        {
            packet.ExtensionFields.CopyTo(destination.Slice(HeaderSize));
        }

        return HeaderSize + packet.ExtensionFields.Length;
    }

    public static uint ConvertReferenceIdToUint(string identifier)
    {
        ReadOnlySpan<byte> bytes = Encoding.ASCII.GetBytes(identifier.PadRight(4, '\0'));
        return BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(0, 4));
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
