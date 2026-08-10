using System;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

using Astrolabed.Dns.Core;

namespace Astrolabed.Dns.RuleEngine;

internal sealed class BlockResponseBuilder
{
    private readonly DnsForwarderOptions _options;
    private readonly string _mode;
    private readonly IPAddress? _staticIp;
    private readonly byte[]? _staticIpBytes;

    public BlockResponseBuilder(DnsForwarderOptions options)
    {
        _options = options;
        _mode = options.BlockResponse.Mode?.ToUpperInvariant() ?? "NXDOMAIN";

        if (IPAddress.TryParse(options.BlockResponse.StaticIp, out var ip))
        {
            _staticIp = ip;
            _staticIpBytes = ip.GetAddressBytes();
        }
    }

    public byte[] BuildBlockResponse(byte[] request)
    {
        return _mode switch
        {
            "NXDOMAIN" => BuildRcodeResponse(request, rcode: 3),
            "SERVFAIL" => BuildRcodeResponse(request, rcode: 2),
            "REFUSED" => BuildRcodeResponse(request, rcode: 5),
            "STATIC_IP" => _staticIpBytes != null && _staticIp != null
                ? BuildStaticIpResponse(request, _staticIp, _staticIpBytes)
                : BuildRcodeResponse(request, rcode: 3),
            _ => BuildRcodeResponse(request, rcode: 3)
        };
    }

    private static byte[] BuildRcodeResponse(byte[] req, int rcode)
    {
        if (req.Length < 12)
        {
            return Array.Empty<byte>();
        }

        var resp = new byte[req.Length];
        Buffer.BlockCopy(req, 0, resp, 0, req.Length);

        // DNS Response Header Patching
        resp[2] = 0x81;
        resp[3] = (byte)(0x80 | (rcode & 0x0F));
        resp[4] = 0x00; resp[5] = 0x01; // QDCOUNT = 1
        resp[6] = 0x00; resp[7] = 0x00; // ANCOUNT = 0
        resp[8] = 0x00; resp[9] = 0x00; // NSCOUNT = 0
        resp[10] = 0x00; resp[11] = 0x00; // ARCOUNT = 0

        return resp;
    }

    private byte[] BuildStaticIpResponse(byte[] req, IPAddress ip, byte[] addrBytes)
    {
        if (req.Length < 12)
        {
            return Array.Empty<byte>();
        }

        int recordLen = 12 + addrBytes.Length; // 2 (pointer) + 2 (type) + 2 (class) + 4 (ttl) + 2 (rdlen) + ip bytes
        var resp = new byte[req.Length + recordLen];

        Buffer.BlockCopy(req, 0, resp, 0, req.Length);

        // DNS Response Header Patching
        resp[2] = 0x81;
        resp[3] = 0x80; // RCODE = 0 (NOERROR)
        resp[4] = 0x00; resp[5] = 0x01; // QDCOUNT = 1
        resp[6] = 0x00; resp[7] = 0x01; // ANCOUNT = 1
        resp[8] = 0x00; resp[9] = 0x00; // NSCOUNT = 0
        resp[10] = 0x00; resp[11] = 0x00; // ARCOUNT = 0

        // Answer Section
        int offset = req.Length;
        resp[offset++] = 0xC0; // Name pointer
        resp[offset++] = 0x0C;

        // Type (A = 0x0001, AAAA = 0x001C)
        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            resp[offset++] = 0x00;
            resp[offset++] = 0x01;
        }
        else
        {
            resp[offset++] = 0x00;
            resp[offset++] = 0x1C;
        }

        // Class (IN = 0x0001)
        resp[offset++] = 0x00;
        resp[offset++] = 0x01;

        // TTL
        BinaryPrimitives.WriteInt32BigEndian(resp.AsSpan(offset, 4), _options.BlockResponse.Ttl);
        offset += 4;

        // Data Length
        resp[offset++] = 0x00;
        resp[offset++] = (byte)addrBytes.Length;

        // Address Bytes
        Buffer.BlockCopy(addrBytes, 0, resp, offset, addrBytes.Length);

        return resp;
    }

    public static byte[] BuildServfail(byte[] req)
    {
        return BuildRcodeResponse(req, rcode: 2);
    }
}
