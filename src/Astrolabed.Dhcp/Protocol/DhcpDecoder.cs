using System.Buffers.Binary;
using System.Net;

namespace Astrolabed.Dhcp.Protocol;

public class DhcpDecoder
{
    private static readonly byte[] MagicCookie = { 0x63, 0x82, 0x53, 0x63 };

    public DhcpMessage Decode(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < 236)
        {
            throw new ArgumentException("Buffer size is smaller than standard DHCP header.", nameof(buffer));
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

        buffer.Slice(28, 16).CopyTo(message.ClientHardwareAddress);

        byte overloadFlags = 0;
        int offset = 236;

        if (buffer.Length >= offset + 4 && buffer.Slice(offset, 4).SequenceEqual(MagicCookie))
        {
            offset += 4;
            overloadFlags = ReadOptionsFromSpan(buffer.Slice(offset), message.Options);
        }

        if ((overloadFlags & 1) != 0 && buffer.Length >= 236)
        {
            ReadOptionsFromSpan(buffer.Slice(108, 128), message.Options);
        }

        if ((overloadFlags & 2) != 0 && buffer.Length >= 108)
        {
            ReadOptionsFromSpan(buffer.Slice(44, 64), message.Options);
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
                break;
            }

            byte[] data = optionBuffer.Slice(offset, length).ToArray();
            var optionCode = (DhcpOptionCode)code;

            if (optionCode == DhcpOptionCode.OptionOverload && data.Length > 0)
            {
                overloadValue = data[0];
            }

            options.Add(new DhcpOption(optionCode, data));
            offset += length;
        }

        return overloadValue;
    }
}
