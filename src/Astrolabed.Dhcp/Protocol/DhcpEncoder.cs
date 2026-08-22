using System.Buffers.Binary;

namespace Astrolabed.Dhcp.Protocol;

public class DhcpEncoder
{
    private static readonly byte[] MagicCookie = { 0x63, 0x82, 0x53, 0x63 };

    public byte[] Encode(DhcpMessage message)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write((byte)message.Operation);
        writer.Write(message.HardwareType);
        writer.Write(message.HardwareAddressLength);
        writer.Write(message.Hops);

        byte[] txIdBytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(txIdBytes, message.TransactionId);
        writer.Write(txIdBytes);

        byte[] secsBytes = new byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(secsBytes, message.Seconds);
        writer.Write(secsBytes);

        byte[] flagsBytes = new byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(flagsBytes, message.Flags);
        writer.Write(flagsBytes);

        writer.Write(message.ClientIpAddress.GetAddressBytes());
        writer.Write(message.YourIpAddress.GetAddressBytes());
        writer.Write(message.ServerIpAddress.GetAddressBytes());
        writer.Write(message.GatewayIpAddress.GetAddressBytes());

        byte[] chaddr = new byte[16];
        Array.Copy(message.ClientHardwareAddress, chaddr, Math.Min(message.ClientHardwareAddress.Length, 16));
        writer.Write(chaddr);

        byte[] sname = new byte[64];
        writer.Write(sname);

        byte[] file = new byte[128];
        writer.Write(file);

        writer.Write(MagicCookie);

        foreach (var option in message.Options)
        {
            if (option.Code == DhcpOptionCode.Pad || option.Code == DhcpOptionCode.End)
            {
                continue;
            }

            writer.Write((byte)option.Code);
            writer.Write((byte)option.Data.Length);
            writer.Write(option.Data);
        }

        writer.Write((byte)DhcpOptionCode.End);

        return stream.ToArray();
    }
}
