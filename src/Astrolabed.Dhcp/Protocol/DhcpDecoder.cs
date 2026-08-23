using System.Buffers.Binary;
using System.Net;

namespace Astrolabed.Dhcp.Protocol;

/// <summary>
/// High-performance, zero-allocation binary decoder for RFC 2131 compliant DHCP network packets.
/// </summary>
public static class DhcpDecoder
{
    private const int MinimumHeaderSize = 236;
    private const int MagicCookieSize = 4;
    private const int SNameOffset = 44;
    private const int SNameLength = 64;
    private const int FileOffset = 108;
    private const int FileLength = 128;
    private const int OptionsOffset = 240;

    private static ReadOnlySpan<byte> MagicCookie => [0x63, 0x82, 0x53, 0x63];

    /// <summary>
    /// Decodes a raw binary byte span into a structured <see cref="DhcpMessage"/>.
    /// </summary>
    /// <param name="buffer">The binary buffer containing the incoming DHCP packet payload.</param>
    /// <returns>A fully parsed <see cref="DhcpMessage"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the buffer length is smaller than the standard DHCP header.</exception>
    public static DhcpMessage Decode(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < MinimumHeaderSize)
        {
            throw new ArgumentException(
                $"Buffer size ({buffer.Length} bytes) is smaller than the required standard DHCP header size of {MinimumHeaderSize} bytes.",
                nameof(buffer));
        }

        var message = new DhcpMessage
        {
            Operation = (DhcpOpCode)buffer[0],
            HardwareType = buffer[1],
            HardwareAddressLength = buffer[2],
            Hops = buffer[3],
            TransactionId = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(4, 4)),
            Seconds = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(8, 2)),
            Flags = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(10, 2)),
            ClientIpAddress = new IPAddress(buffer.Slice(12, 4)),
            YourIpAddress = new IPAddress(buffer.Slice(16, 4)),
            ServerIpAddress = new IPAddress(buffer.Slice(20, 4)),
            GatewayIpAddress = new IPAddress(buffer.Slice(24, 4))
        };

        // Copy Client Hardware Address safely taking HardwareAddressLength into account
        int chaddrLength = Math.Min((int)message.HardwareAddressLength, 16);
        buffer.Slice(28, chaddrLength).CopyTo(message.ClientHardwareAddress);

        byte overloadFlags = 0;

        // Process Magic Cookie and Standard Options Field
        if (buffer.Length >= OptionsOffset && buffer.Slice(MinimumHeaderSize, MagicCookieSize).SequenceEqual(MagicCookie))
        {
            overloadFlags = ReadOptionsFromSpan(buffer[OptionsOffset..], message.Options);
        }

        // RFC 2131: Option Overload Processing
        // 1 = 'file' field holds options
        // 2 = 'sname' field holds options
        // 3 = both 'file' and 'sname' fields hold options
        if ((overloadFlags & 1) != 0 && buffer.Length >= FileOffset + FileLength)
        {
            ReadOptionsFromSpan(buffer.Slice(FileOffset, FileLength), message.Options);
        }

        if ((overloadFlags & 2) != 0 && buffer.Length >= SNameOffset + SNameLength)
        {
            ReadOptionsFromSpan(buffer.Slice(SNameOffset, SNameLength), message.Options);
        }

        return message;
    }

    private static byte ReadOptionsFromSpan(ReadOnlySpan<byte> optionBuffer, List<DhcpOption> options)
    {
        byte overloadValue = 0;
        int offset = 0;

        while (offset < optionBuffer.Length)
        {
            byte code = optionBuffer[offset++];

            if (code == (byte)DhcpOptionCode.Pad)
            {
                continue;
            }

            if (code == (byte)DhcpOptionCode.End)
            {
                break;
            }

            if (offset >= optionBuffer.Length)
            {
                break;
            }

            byte length = optionBuffer[offset++];

            if (offset + length > optionBuffer.Length)
            {
                // Truncated option encountered; halt parsing safely
                break;
            }

            ReadOnlySpan<byte> optionDataSpan = optionBuffer.Slice(offset, length);
            var optionCode = (DhcpOptionCode)code;

            if (optionCode == DhcpOptionCode.OptionOverload && optionDataSpan.Length > 0)
            {
                overloadValue = optionDataSpan[0];
            }

            options.Add(new DhcpOption(optionCode, optionDataSpan.ToArray()));
            offset += length;
        }

        return overloadValue;
    }
}

