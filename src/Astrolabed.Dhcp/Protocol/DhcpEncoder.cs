using System.Buffers.Binary;

namespace Astrolabed.Dhcp.Protocol;

public class DhcpEncoder
{
    private static readonly byte[] MagicCookie = { 0x63, 0x82, 0x53, 0x63 };

    public byte[] Encode(DhcpMessage message)
    {
        ushort maxMessageSize = ExtractMaxMessageSize(message);
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
        byte[] file = new byte[128];

        var normalOptions = new List<DhcpOption>();
        var fileOptions = new List<DhcpOption>();
        var snameOptions = new List<DhcpOption>();

        int estimatedSize = 240 + CalculateOptionsLength(message.Options) + 1;
        byte overloadFlag = 0;

        if (estimatedSize > maxMessageSize || estimatedSize > 576)
        {
            PartitionOptions(message.Options, normalOptions, fileOptions, snameOptions, out overloadFlag);
        }
        else
        {
            normalOptions.AddRange(message.Options);
        }

        if (overloadFlag != 0)
        {
            normalOptions.Insert(0, DhcpOption.CreateByte(DhcpOptionCode.OptionOverload, overloadFlag));
            EncodeOptionsToBuffer(fileOptions, file);
            EncodeOptionsToBuffer(snameOptions, sname);
        }

        writer.Write(sname);
        writer.Write(file);
        writer.Write(MagicCookie);

        foreach (var option in normalOptions)
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

        byte[] encodedPacket = stream.ToArray();
        if (encodedPacket.Length > maxMessageSize)
        {
            Array.Resize(ref encodedPacket, maxMessageSize);
        }

        return encodedPacket;
    }

    private static ushort ExtractMaxMessageSize(DhcpMessage message)
    {
        var maxMsgSizeOpt = message.Options.FirstOrDefault(o => o.Code == DhcpOptionCode.MaximumDhcpMessageSize);
        if (maxMsgSizeOpt != null && maxMsgSizeOpt.Data.Length >= 2)
        {
            ushort requestedSize = BinaryPrimitives.ReadUInt16BigEndian(maxMsgSizeOpt.Data);
            return Math.Max((ushort)576, requestedSize);
        }
        return 576;
    }

    private static int CalculateOptionsLength(IEnumerable<DhcpOption> options)
    {
        return options.Where(o => o.Code != DhcpOptionCode.Pad && o.Code != DhcpOptionCode.End)
                      .Sum(o => 2 + o.Data.Length);
    }

    private static void PartitionOptions(List<DhcpOption> source, List<DhcpOption> normal, List<DhcpOption> file, List<DhcpOption> sname, out byte overloadFlag)
    {
        overloadFlag = 0;
        int currentNormalSize = 3;

        foreach (var option in source)
        {
            int optSize = 2 + option.Data.Length;
            if (currentNormalSize + optSize <= 308)
            {
                normal.Add(option);
                currentNormalSize += optSize;
            }
            else if (CalculateOptionsLength(file) + optSize <= 126)
            {
                file.Add(option);
                overloadFlag |= 1;
            }
            else if (CalculateOptionsLength(sname) + optSize <= 62)
            {
                sname.Add(option);
                overloadFlag |= 2;
            }
        }
    }

    private static void EncodeOptionsToBuffer(List<DhcpOption> options, byte[] buffer)
    {
        if (options.Count == 0)
        {
            return;
        }

        int offset = 0;
        foreach (var option in options)
        {
            if (offset + 2 + option.Data.Length >= buffer.Length)
            {
                break;
            }

            buffer[offset++] = (byte)option.Code;
            buffer[offset++] = (byte)option.Data.Length;
            Array.Copy(option.Data, 0, buffer, offset, option.Data.Length);
            offset += option.Data.Length;
        }

        if (offset < buffer.Length)
        {
            buffer[offset] = (byte)DhcpOptionCode.End;
        }
    }
}
