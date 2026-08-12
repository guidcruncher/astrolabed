using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Astrolabed.Dhcp;

public static class DhcpPacketCodec
{
    private static readonly byte[] MagicCookie = { 99, 130, 83, 99 };

    public static DhcpPacket Parse(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length < 240)
        {
            throw new ArgumentException("Packet is too short for standard DHCP format", nameof(data));
        }

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
        {
            throw new FormatException("Invalid DHCP magic cookie");
        }

        offset += 4;

        while (offset < data.Length)
        {
            byte code = data[offset++];

            if (code == 255) // End Option
            {
                break;
            }

            if (code == 0) // Pad Option
            {
                continue;
            }

            if (offset >= data.Length)
            {
                break;
            }

            byte len = data[offset++];
            if (offset + len > data.Length)
            {
                break;
            }

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
        IPAddress subnetMask,
        TimeSpan lease,
        IPAddress? ntp = null,
        string? domainName = null,
        ushort? interfaceMtu = null,
        string? tftpServerName = null,
        string? bootfileName = null,
        string? webproxy = null)
    {
        return BuildResponse(
            discover,
            DhcpMessageType.Offer,
            offeredIp,
            serverId,
            router,
            dns,
            subnetMask,
            lease,
            ntp,
            domainName,
            interfaceMtu,
            tftpServerName,
            bootfileName,
            webproxy);
    }

    public static byte[] BuildAck(
        DhcpPacket request,
        IPAddress assignedIp,
        IPAddress serverId,
        IPAddress router,
        IPAddress dns,
        IPAddress subnetMask,
        TimeSpan lease,
        IPAddress? ntp = null,
        string? domainName = null,
        ushort? interfaceMtu = null,
        string? tftpServerName = null,
        string? bootfileName = null,
        string? webproxy = null)
    {
        return BuildResponse(
            request,
            DhcpMessageType.Ack,
            assignedIp,
            serverId,
            router,
            dns,
            subnetMask,
            lease,
            ntp,
            domainName,
            interfaceMtu,
            tftpServerName,
            bootfileName,
            webproxy);
    }

    public static byte[] BuildInformAck(
        DhcpPacket inform,
        IPAddress serverId,
        IPAddress router,
        IPAddress dns,
        IPAddress subnetMask,
        IPAddress? ntp = null,
        string? domainName = null,
        ushort? interfaceMtu = null,
        string? webproxy = null)
    {
        var writer = new ArrayBufferWriter<byte>(512);
        WriteHeader(writer, inform, 2, inform.Ciaddr, IPAddress.Any, serverId, null, null);

        // Option 53: Message Type
        WriteOptionHeader(writer, 53, 1);
        writer.GetSpan(1)[0] = (byte)DhcpMessageType.Ack;
        writer.Advance(1);

        // Option 54: Server Identifier
        WriteOptionIp(writer, 54, serverId);

        // Option 1: Subnet Mask
        WriteOptionIp(writer, 1, subnetMask);

        // Option 3: Router
        WriteOptionIp(writer, 3, router);

        // Option 6: DNS Server
        WriteOptionIp(writer, 6, dns);

        if (!string.IsNullOrWhiteSpace(domainName))
        {
            // Option 15: Domain Name
            WriteOptionString(writer, 15, domainName);
        }

        if (interfaceMtu.HasValue)
        {
            // Option 26: Interface MTU
            WriteOptionUInt16(writer, 26, interfaceMtu.Value);
        }

        if (ntp is not null)
        {
            // Option 42: NTP Server
            WriteOptionIp(writer, 42, ntp);
        }

        if (!string.IsNullOrWhiteSpace(webproxy))
        {
            // Option 252: WPAD URL
            WriteOptionString(writer, 252, webproxy);
        }

        writer.GetSpan(1)[0] = 255; // End Option
        writer.Advance(1);

        return writer.WrittenSpan.ToArray();
    }

    public static byte[] BuildNak(DhcpPacket request, IPAddress serverId)
    {
        var writer = new ArrayBufferWriter<byte>(300);
        WriteHeader(writer, request, 2, request.Ciaddr, IPAddress.Any, serverId, null, null);

        // Option 53: Message Type
        WriteOptionHeader(writer, 53, 1);
        writer.GetSpan(1)[0] = (byte)DhcpMessageType.Nak;
        writer.Advance(1);

        // Option 54: Server Identifier
        WriteOptionIp(writer, 54, serverId);

        writer.GetSpan(1)[0] = 255; // End Option
        writer.Advance(1);

        return writer.WrittenSpan.ToArray();
    }

    private static byte[] BuildResponse(
        DhcpPacket req,
        DhcpMessageType type,
        IPAddress yiaddr,
        IPAddress serverId,
        IPAddress router,
        IPAddress dns,
        IPAddress subnetMask,
        TimeSpan lease,
        IPAddress? ntp = null,
        string? domainName = null,
        ushort? interfaceMtu = null,
        string? tftpServerName = null,
        string? bootfileName = null,
        string? webproxy = null)
    {
        var writer = new ArrayBufferWriter<byte>(512);
        WriteHeader(writer, req, 2, req.Ciaddr, yiaddr, serverId, tftpServerName, bootfileName);

        // Option 53: Message Type
        WriteOptionHeader(writer, 53, 1);
        writer.GetSpan(1)[0] = (byte)type;
        writer.Advance(1);

        // Option 54: Server Identifier
        WriteOptionIp(writer, 54, serverId);

        // Option 51: IP Address Lease Time
        uint leaseSeconds = (uint)lease.TotalSeconds;
        WriteOptionUInt32(writer, 51, leaseSeconds);

        // Option 58: Renewal (T1) Time (50% of lease duration)
        WriteOptionUInt32(writer, 58, (uint)(leaseSeconds * 0.50));

        // Option 59: Rebinding (T2) Time (87.5% of lease duration)
        WriteOptionUInt32(writer, 59, (uint)(leaseSeconds * 0.875));

        // Option 1: Subnet Mask
        WriteOptionIp(writer, 1, subnetMask);

        // Option 3: Router
        WriteOptionIp(writer, 3, router);

        // Option 6: DNS Server
        WriteOptionIp(writer, 6, dns);

        if (!string.IsNullOrWhiteSpace(domainName))
        {
            // Option 15: Domain Name
            WriteOptionString(writer, 15, domainName);
        }

        if (interfaceMtu.HasValue)
        {
            // Option 26: Interface MTU
            WriteOptionUInt16(writer, 26, interfaceMtu.Value);
        }

        if (ntp is not null)
        {
            // Option 42: NTP Server
            WriteOptionIp(writer, 42, ntp);
        }

        if (!string.IsNullOrWhiteSpace(tftpServerName))
        {
            // Option 66: TFTP Server Name
            WriteOptionString(writer, 66, tftpServerName);
        }

        if (!string.IsNullOrWhiteSpace(bootfileName))
        {
            // Option 67: Bootfile Name
            WriteOptionString(writer, 67, bootfileName);
        }

        if (!string.IsNullOrWhiteSpace(webproxy))
        {
            // Option 252: WPAD URL
            WriteOptionString(writer, 252, webproxy);
        }

        writer.GetSpan(1)[0] = 255; // End Option
        writer.Advance(1);

        return writer.WrittenSpan.ToArray();
    }

    private static void WriteHeader(
        IBufferWriter<byte> writer,
        DhcpPacket req,
        byte op,
        IPAddress ciaddr,
        IPAddress yiaddr,
        IPAddress serverId,
        string? sname,
        string? file)
    {
        Span<byte> span = writer.GetSpan(236);

        span[0] = op;
        span[1] = req.Htype;
        span[2] = req.Hlen;
        span[3] = req.Hops;

        BinaryPrimitives.WriteUInt32BigEndian(span[4..], req.Xid);
        BinaryPrimitives.WriteUInt16BigEndian(span[8..], req.Secs);
        BinaryPrimitives.WriteUInt16BigEndian(span[10..], req.Flags);

        ciaddr.TryWriteBytes(span[12..], out _);
        yiaddr.TryWriteBytes(span[16..], out _);
        serverId.TryWriteBytes(span[20..], out _);
        req.Giaddr.TryWriteBytes(span[24..], out _);

        span[28..44].Clear();
        req.Chaddr.AsSpan(0, Math.Min(req.Chaddr.Length, 16)).CopyTo(span[28..]);

        span[44..108].Clear();  // sname (64 bytes)
        if (!string.IsNullOrEmpty(sname))
        {
            Encoding.ASCII.GetBytes(sname.AsSpan(0, Math.Min(sname.Length, 63)), span[44..108]);
        }

        span[108..236].Clear(); // file (128 bytes)
        if (!string.IsNullOrEmpty(file))
        {
            Encoding.ASCII.GetBytes(file.AsSpan(0, Math.Min(file.Length, 127)), span[108..236]);
        }

        writer.Advance(236);

        Span<byte> cookieSpan = writer.GetSpan(4);
        MagicCookie.CopyTo(cookieSpan);
        writer.Advance(4);
    }

    private static void WriteOptionHeader(IBufferWriter<byte> writer, byte code, byte length)
    {
        Span<byte> span = writer.GetSpan(2);
        span[0] = code;
        span[1] = length;
        writer.Advance(2);
    }

    private static void WriteOptionIp(IBufferWriter<byte> writer, byte code, IPAddress ip)
    {
        WriteOptionHeader(writer, code, 4);
        Span<byte> span = writer.GetSpan(4);
        ip.TryWriteBytes(span, out _);
        writer.Advance(4);
    }

    private static void WriteOptionUInt16(IBufferWriter<byte> writer, byte code, ushort value)
    {
        WriteOptionHeader(writer, code, 2);
        Span<byte> span = writer.GetSpan(2);
        BinaryPrimitives.WriteUInt16BigEndian(span, value);
        writer.Advance(2);
    }

    private static void WriteOptionUInt32(IBufferWriter<byte> writer, byte code, uint value)
    {
        WriteOptionHeader(writer, code, 4);
        Span<byte> span = writer.GetSpan(4);
        BinaryPrimitives.WriteUInt32BigEndian(span, value);
        writer.Advance(4);
    }

    private static void WriteOptionString(IBufferWriter<byte> writer, byte code, string value)
    {
        int byteCount = Encoding.UTF8.GetByteCount(value);
        byte length = (byte)Math.Min(byteCount, 255);

        WriteOptionHeader(writer, code, length);
        Span<byte> span = writer.GetSpan(length);
        Encoding.UTF8.GetBytes(value.AsSpan(0, Math.Min(value.Length, length)), span);
        writer.Advance(length);
    }
}
