using System;
using System.Buffers.Binary;
using System.Net;

using Astrolabed.Dns.Core;

using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.RuleEngine;

internal sealed class BlockResponseBuilder
{
    private readonly DnsForwarderOptions _options;

    public BlockResponseBuilder(IOptions<DnsForwarderOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    public byte[] BuildBlockResponse(byte[] rawRequest)
    {
        if (rawRequest == null || rawRequest.Length < 12)
        {
            return BuildServfail(rawRequest ?? Array.Empty<byte>());
        }

        string mode = _options.BlockResponse?.Mode ?? "NxDomain";

        return mode.ToLowerInvariant() switch
        {
            "nxdomain" => BuildRcodeResponse(rawRequest, 3), // NXDOMAIN
            "refused" => BuildRcodeResponse(rawRequest, 5),  // REFUSED
            "zeroip" => BuildZeroIpResponse(rawRequest),
            "customip" => BuildCustomIpResponse(rawRequest),
            _ => BuildRcodeResponse(rawRequest, 3)
        };
    }

    public static byte[] BuildServfail(byte[] rawRequest)
    {
        return BuildRcodeResponse(rawRequest, 2); // SERVFAIL
    }

    private static byte[] BuildRcodeResponse(byte[] rawRequest, byte rcode)
    {
        byte[] response = new byte[rawRequest.Length];
        Array.Copy(rawRequest, response, rawRequest.Length);

        // Set QR bit = 1 (Response)
        response[2] |= 0x80;

        // Set RCODE in header lower 4 bits of byte 3
        response[3] = (byte)((response[3] & 0xF0) | (rcode & 0x0F));

        return response;
    }

    private byte[] BuildZeroIpResponse(byte[] rawRequest)
    {
        var msg = DnsMessage.TryParse(rawRequest);
        if (msg == null)
        {
            return BuildRcodeResponse(rawRequest, 3);
        }

        string qType = msg.QuestionType;

        if (string.Equals(qType, "A", StringComparison.OrdinalIgnoreCase) || qType == "1")
        {
            return BuildIpAnswer(rawRequest, msg, IPAddress.Any);
        }

        if (string.Equals(qType, "AAAA", StringComparison.OrdinalIgnoreCase) || qType == "28")
        {
            return BuildIpAnswer(rawRequest, msg, IPAddress.IPv6Any);
        }

        return BuildRcodeResponse(rawRequest, 3);
    }

    private byte[] BuildCustomIpResponse(byte[] rawRequest)
    {
        var msg = DnsMessage.TryParse(rawRequest);
        if (msg == null)
        {
            return BuildRcodeResponse(rawRequest, 3);
        }

        string customIp = _options.BlockResponse?.StaticIp ?? "0.0.0.0";
        if (!IPAddress.TryParse(customIp, out var ip))
        {
            ip = IPAddress.Any;
        }

        string qType = msg.QuestionType;

        if ((string.Equals(qType, "A", StringComparison.OrdinalIgnoreCase) || qType == "1") &&
            ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return BuildIpAnswer(rawRequest, msg, ip);
        }

        if ((string.Equals(qType, "AAAA", StringComparison.OrdinalIgnoreCase) || qType == "28") &&
            ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            return BuildIpAnswer(rawRequest, msg, ip);
        }

        return BuildRcodeResponse(rawRequest, 3);
    }

    private static byte[] BuildIpAnswer(byte[] rawRequest, DnsMessage msg, IPAddress ip)
    {
        byte[] ipBytes = ip.GetAddressBytes();
        int answerLength = 2 + 2 + 2 + 4 + 2 + ipBytes.Length; // Pointer, Type, Class, TTL, RDataLen, RData
        byte[] response = new byte[rawRequest.Length + answerLength];

        Array.Copy(rawRequest, response, rawRequest.Length);

        // Header: QR=1, RA=1, ANCOUNT=1
        response[2] |= 0x80;
        response[3] |= 0x80;
        BinaryPrimitives.WriteUInt16BigEndian(response.AsSpan(6, 2), 1);

        int offset = rawRequest.Length;

        // Compression pointer to Question Name (0xC00C)
        response[offset++] = 0xC0;
        response[offset++] = 0x0C;

        // Type
        ushort typeCode = string.Equals(msg.QuestionType, "AAAA", StringComparison.OrdinalIgnoreCase) || msg.QuestionType == "28"
            ? (ushort)28
            : (ushort)1;

        BinaryPrimitives.WriteUInt16BigEndian(response.AsSpan(offset, 2), typeCode);
        offset += 2;

        // Class IN (1)
        BinaryPrimitives.WriteUInt16BigEndian(response.AsSpan(offset, 2), 1);
        offset += 2;

        // TTL (60s)
        BinaryPrimitives.WriteUInt32BigEndian(response.AsSpan(offset, 4), 60);
        offset += 4;

        // RData Length
        BinaryPrimitives.WriteUInt16BigEndian(response.AsSpan(offset, 2), (ushort)ipBytes.Length);
        offset += 2;

        // RData
        ipBytes.CopyTo(response, offset);

        return response;
    }
}
