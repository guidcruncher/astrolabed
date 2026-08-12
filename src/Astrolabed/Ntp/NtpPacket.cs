using System;
using System.Buffers.Binary;

namespace Astrolabed.Ntp;

public sealed class NtpPacket
{
    public byte LeapIndicator { get; set; }
    public byte Version { get; set; } = 4;
    public byte Mode { get; set; } = 4;
    public byte Stratum { get; set; } = 2;
    public byte Poll { get; set; } = 6;
    public sbyte Precision { get; set; } = -20;

    public uint RootDelay { get; set; }
    public uint RootDispersion { get; set; }
    public uint ReferenceId { get; set; }

    public DateTime ReferenceTimestamp { get; set; }
    public uint OriginateTimestampSeconds { get; set; }
    public uint OriginateTimestampFraction { get; set; }

    public uint ReceiveTimestampSeconds { get; set; }
    public uint ReceiveTimestampFraction { get; set; }

    public uint TransmitTimestampSeconds { get; set; }
    public uint TransmitTimestampFraction { get; set; }

    public static NtpPacket Parse(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < 48)
        {
            throw new ArgumentException("NTP packet must be at least 48 bytes.", nameof(buffer));
        }

        return new NtpPacket
        {
            LeapIndicator = (byte)((buffer[0] >> 6) & 0x03),
            Version = (byte)((buffer[0] >> 3) & 0x07),
            Mode = (byte)(buffer[0] & 0x07),
            Stratum = buffer[1],
            Poll = buffer[2],
            Precision = (sbyte)buffer[3],
            RootDelay = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(4, 4)),
            RootDispersion = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(8, 4)),
            ReferenceId = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(12, 4)),
            ReferenceTimestamp = NtpTimestamp.ReadTimestamp(buffer.Slice(16, 8)),
            OriginateTimestampSeconds = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(24, 4)),
            OriginateTimestampFraction = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(28, 4)),
            ReceiveTimestampSeconds = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(32, 4)),
            ReceiveTimestampFraction = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(36, 4)),
            TransmitTimestampSeconds = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(40, 4)),
            TransmitTimestampFraction = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(44, 4))
        };
    }

    public static NtpPacket BuildResponse(
        NtpPacket request,
        DateTime receiveUtc,
        DateTime transmitUtc,
        int stratum = 2,
        uint referenceId = 0x4C4F434C,
        byte leapIndicator = 0,
        DateTime? referenceUtc = null)
    {
        Span<byte> rxBuf = stackalloc byte[8];
        Span<byte> txBuf = stackalloc byte[8];

        NtpTimestamp.WriteTimestamp(rxBuf, receiveUtc);
        NtpTimestamp.WriteTimestamp(txBuf, transmitUtc);

        return new NtpPacket
        {
            LeapIndicator = leapIndicator,
            Version = request.Version is >= 1 and <= 4 ? request.Version : (byte)4,
            Mode = 4, // Server Mode
            Stratum = (byte)stratum,
            Poll = request.Poll,
            Precision = -20,
            ReferenceId = referenceId,
            ReferenceTimestamp = referenceUtc ?? receiveUtc,
            OriginateTimestampSeconds = request.TransmitTimestampSeconds,
            OriginateTimestampFraction = request.TransmitTimestampFraction,
            ReceiveTimestampSeconds = BinaryPrimitives.ReadUInt32BigEndian(rxBuf[..4]),
            ReceiveTimestampFraction = BinaryPrimitives.ReadUInt32BigEndian(rxBuf.Slice(4, 4)),
            TransmitTimestampSeconds = BinaryPrimitives.ReadUInt32BigEndian(txBuf[..4]),
            TransmitTimestampFraction = BinaryPrimitives.ReadUInt32BigEndian(txBuf.Slice(4, 4))
        };
    }

    public byte[] ToBytes()
    {
        var buffer = new byte[48];

        buffer[0] = (byte)(((LeapIndicator & 0x03) << 6) | ((Version & 0x07) << 3) | (Mode & 0x07));
        buffer[1] = Stratum;
        buffer[2] = Poll;
        buffer[3] = (byte)Precision;

        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(4, 4), RootDelay);
        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(8, 4), RootDispersion);
        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(12, 4), ReferenceId);

        NtpTimestamp.WriteTimestamp(buffer.AsSpan(16, 8), ReferenceTimestamp);

        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(24, 4), OriginateTimestampSeconds);
        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(28, 4), OriginateTimestampFraction);
        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(32, 4), ReceiveTimestampSeconds);
        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(36, 4), ReceiveTimestampFraction);
        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(40, 4), TransmitTimestampSeconds);
        BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(44, 4), TransmitTimestampFraction);

        return buffer;
    }
}
