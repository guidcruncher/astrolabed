using System.Buffers.Binary;
using System.Net;

namespace Astrolabed.Dhcp;

public static class DhcpPacketCodec
{
    private static readonly byte[] MagicCookie = { 99, 130, 83, 99 };

    public static byte[] BuildInformAck(
        DhcpPacket inform,
        IPAddress serverId,
        IPAddress router,
        IPAddress dns,
        IPAddress? ntp = null,
	IPAddress? webproxy = null,
        IPAddress? subnetMask = null)
    {
        var buf = new List<byte>();

        buf.Add(2);
        buf.Add(inform.Htype);
        buf.Add(inform.Hlen);
        buf.Add(inform.Hops);

        AppendUInt32BigEndian(buf, inform.Xid);
        AppendUInt16BigEndian(buf, inform.Secs);
        AppendUInt16BigEndian(buf, inform.Flags);

        buf.AddRange(inform.Ciaddr.GetAddressBytes());
        buf.AddRange(IPAddress.Any.GetAddressBytes());
        buf.AddRange(serverId.GetAddressBytes());
        buf.AddRange(inform.Giaddr.GetAddressBytes());

        buf.AddRange(inform.Chaddr);
        buf.AddRange(new byte[64]);
        buf.AddRange(new byte[128]);

        buf.AddRange(MagicCookie);

        buf.Add(53);
        buf.Add(1);
        buf.Add((byte)DhcpMessageType.Ack);

        buf.Add(54);
        buf.Add(4);
        buf.AddRange(serverId.GetAddressBytes());

        if (subnetMask != null)
        {
            buf.Add(1);
            buf.Add(4);
            buf.AddRange(subnetMask.GetAddressBytes());
        }

        buf.Add(3);
        buf.Add(4);
        buf.AddRange(router.GetAddressBytes());

        buf.Add(6);
        buf.Add(4);
        buf.AddRange(dns.GetAddressBytes());

        if (ntp is not null)
        {
            buf.Add(42);
            buf.Add(4);
            buf.AddRange(ntp.GetAddressBytes());
        }

        if (webproxy is not null)
        {
            buf.Add(252);
            buf.Add(4);
            buf.AddRange(webproxy.GetAddressBytes());
        }

        buf.Add(255);

        return buf.ToArray();
    }

    public static byte[] BuildNak(DhcpPacket request, IPAddress serverId)
    {
        var buf = new List<byte>();

        buf.Add(2);
        buf.Add(request.Htype);
        buf.Add(request.Hlen);
        buf.Add(request.Hops);

        AppendUInt32BigEndian(buf, request.Xid);
        AppendUInt16BigEndian(buf, request.Secs);
        AppendUInt16BigEndian(buf, request.Flags);

        buf.AddRange(request.Ciaddr.GetAddressBytes());
        buf.AddRange(IPAddress.Any.GetAddressBytes());
        buf.AddRange(serverId.GetAddressBytes());
        buf.AddRange(request.Giaddr.GetAddressBytes());

        buf.AddRange(request.Chaddr);
        buf.AddRange(new byte[64]);
        buf.AddRange(new byte[128]);

        buf.AddRange(MagicCookie);

        buf.Add(53);
        buf.Add(1);
        buf.Add((byte)DhcpMessageType.Nak);

        buf.Add(54);
        buf.Add(4);
        buf.AddRange(serverId.GetAddressBytes());

        buf.Add(255);

        return buf.ToArray();
    }

    public static DhcpPacket Parse(byte[] data)
    {
        if (data.Length < 240)
            throw new ArgumentException("Packet is too short for standard DHCP format", nameof(data));

        var p = new DhcpPacket
        {
            Op = data[0],
            Htype = data[1],
            Hlen = data[2],
            Hops = data[3],
            Xid = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(4, 4)),
            Secs = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(8, 2)),
            Flags = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(10, 2)),
            Ciaddr = new IPAddress(data.AsSpan(12, 4)),
            Yiaddr = new IPAddress(data.AsSpan(16, 4)),
            Siaddr = new IPAddress(data.AsSpan(20, 4)),
            Giaddr = new IPAddress(data.AsSpan(24, 4)),
            Chaddr = data.AsSpan(28, 16).ToArray()
        };

        int offset = 236;

        if (!data.AsSpan(offset, 4).SequenceEqual(MagicCookie))
            throw new FormatException("Invalid DHCP magic cookie");

        offset += 4;

        while (offset < data.Length)
        {
            byte code = data[offset++];

            if (code == 255)
                break;

            if (code == 0)
                continue;

            if (offset >= data.Length)
                break;

            byte len = data[offset++];
            if (offset + len > data.Length)
                break;

            var optData = data.AsSpan(offset, len).ToArray();
            offset += len;

            p.Options.Add(new DhcpOption(code, optData));
        }

        return p;
    }

    public static byte[] BuildOffer(
        DhcpPacket discover,
        IPAddress offeredIp,
        IPAddress serverId,
        IPAddress router,
        IPAddress dns,
        IPAddress? ntp,
	IPAddress? webproxy,
        TimeSpan lease,
        IPAddress subnetMask)
    {
        return BuildResponse(
            discover,
            DhcpMessageType.Offer,
            offeredIp,
            serverId,
            router,
            dns,
            ntp,
	    webproxy,
            lease,
            subnetMask);
    }

    public static byte[] BuildAck(
        DhcpPacket request,
        IPAddress assignedIp,
        IPAddress serverId,
        IPAddress router,
        IPAddress dns,
        IPAddress? ntp,
	IPAddress? webproxy,
        TimeSpan lease,
        IPAddress subnetMask)
    {
        return BuildResponse(
            request,
            DhcpMessageType.Ack,
            assignedIp,
            serverId,
            router,
            dns,
            ntp,
	    webproxy,
            lease,
            subnetMask);
    }

    private static byte[] BuildResponse(
        DhcpPacket req,
        DhcpMessageType type,
        IPAddress yiaddr,
        IPAddress serverId,
        IPAddress router,
        IPAddress dns,
        IPAddress? ntp,
	IPAddress? webproxy,
        TimeSpan lease,
        IPAddress subnetMask)
    {
        var buf = new List<byte>();

        buf.Add(2);
        buf.Add(req.Htype);
        buf.Add(req.Hlen);
        buf.Add(req.Hops);

        AppendUInt32BigEndian(buf, req.Xid);
        AppendUInt16BigEndian(buf, req.Secs);
        AppendUInt16BigEndian(buf, req.Flags);

        buf.AddRange(req.Ciaddr.GetAddressBytes());
        buf.AddRange(yiaddr.GetAddressBytes());
        buf.AddRange(serverId.GetAddressBytes());
        buf.AddRange(req.Giaddr.GetAddressBytes());

        buf.AddRange(req.Chaddr);
        buf.AddRange(new byte[64]);
        buf.AddRange(new byte[128]);

        buf.AddRange(MagicCookie);

        buf.Add(53);
        buf.Add(1);
        buf.Add((byte)type);

        buf.Add(54);
        buf.Add(4);
        buf.AddRange(serverId.GetAddressBytes());

        buf.Add(51);
        buf.Add(4);
        AppendUInt32BigEndian(buf, (uint)lease.TotalSeconds);

        buf.Add(3);
        buf.Add(4);
        buf.AddRange(router.GetAddressBytes());

        buf.Add(6);
        buf.Add(4);
        buf.AddRange(dns.GetAddressBytes());

        if (ntp is not null)
        {
            buf.Add(42);
            buf.Add(4);
            buf.AddRange(ntp.GetAddressBytes());
        }

        if (webproxy is not null)
        {
            buf.Add(252);
            buf.Add(4);
            buf.AddRange(webproxy.GetAddressBytes());
        }

        buf.Add(1);
        buf.Add(4);
        buf.AddRange(subnetMask.GetAddressBytes());

        buf.Add(255);

        return buf.ToArray();
    }

    private static void AppendUInt16BigEndian(List<byte> buf, ushort value)
    {
        Span<byte> bytes = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(bytes, value);
        buf.Add(bytes[0]);
        buf.Add(bytes[1]);
    }

    private static void AppendUInt32BigEndian(List<byte> buf, uint value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        buf.Add(bytes[0]);
        buf.Add(bytes[1]);
        buf.Add(bytes[2]);
        buf.Add(bytes[3]);
    }
}
