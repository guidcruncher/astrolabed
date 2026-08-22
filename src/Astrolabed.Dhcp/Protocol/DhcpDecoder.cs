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

        int offset = 236;
        if (buffer.Length >= offset + 4 && buffer.Slice(offset, 4).SequenceEqual(MagicCookie))
        {
            offset += 4;
            while (offset < buffer.Length)
            {
                byte code = buffer[offset++];
                if (code == (byte)DhcpOptionCode.Pad)
                {
                    continue;
                }
                if (code == (byte)DhcpOptionCode.End)
                {
                    break;
                }

                if (offset >= buffer.Length)
                {
                    break;
                }

                byte length = buffer[offset++];
                if (offset + length > buffer.Length)
                {
                    break;
                }

                byte[] data = buffer.Slice(offset, length).ToArray();
                message.Options.Add(new DhcpOption((DhcpOptionCode)code, data));
                offset += length;
            }
        }

        return message;
    }
}
