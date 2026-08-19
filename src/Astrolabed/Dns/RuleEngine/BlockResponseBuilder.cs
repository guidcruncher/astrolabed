using System;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

using Astrolabed.Dns.Core;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.RuleEngine;

internal sealed class BlockResponseBuilder
{
    private readonly DnsForwarderOptions _options;

    private const ushort OptionCodeEdnsError = 15;
    private const ushort EdeCodeBlocked = 15;
    private const ushort EdeCodeFiltered = 17;

    public BlockResponseBuilder(
        IOptions<DnsForwarderOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
    }

    public byte[] BuildBlockResponse(byte[] rawRequest, ReadOnlySpan<byte> upstreamEdeBytes = default)
    {
        if (rawRequest == null || rawRequest.Length < 12)
        {
            return BuildServfail(rawRequest ?? Array.Empty<byte>());
        }

        if (!upstreamEdeBytes.IsEmpty)
        {
            byte[] baseResp = BuildRcodeResponse(rawRequest, 2); // SERVFAIL base
            return EdeForwarder.AttachUpstreamEde(baseResp, upstreamEdeBytes);
        }

        string mode = _options.BlockResponse?.Mode ?? "NxDomain";

        return mode.ToLowerInvariant() switch
        {
            "nxdomain" => BuildRcodeResponseWithEde(rawRequest, 3, EdeCodeBlocked),
            "refused" => BuildRcodeResponseWithEde(rawRequest, 5, EdeCodeBlocked),
            "filtered" => BuildRcodeResponseWithEde(rawRequest, 5, EdeCodeFiltered),
            "nodata" => BuildNoDataResponseWithEde(rawRequest, EdeCodeBlocked),
            "servfail" => BuildServfail(rawRequest ?? Array.Empty<byte>()),
            "zeroip" => BuildZeroIpResponse(rawRequest),
            "customip" => BuildCustomIpResponse(rawRequest),
            _ => BuildRcodeResponseWithEde(rawRequest, 3, EdeCodeBlocked)
        };
    }

    public static byte[] BuildServfail(byte[] rawRequest)
    {
        return BuildRcodeResponse(rawRequest, 2);
    }

    private static byte[] BuildNoDataResponseWithEde(byte[] rawRequest, ushort edeCode)
    {
        byte[] baseResponse = BuildRcodeResponse(rawRequest, 0);
        if (baseResponse.Length < 12)
        {
            return baseResponse;
        }

        BinaryPrimitives.WriteUInt16BigEndian(baseResponse.AsSpan(4, 2), 0);
        return AppendEdeOptRecord(baseResponse, edeCode);
    }

    private static byte[] BuildRcodeResponse(byte[] rawRequest, byte rcode)
    {
        if (rawRequest == null || rawRequest.Length < 12)
        {
            return Array.Empty<byte>();
        }

        byte[] response = new byte[rawRequest.Length];
        Array.Copy(rawRequest, response, rawRequest.Length);

        response[2] |= 0x80;
        response[3] = (byte)((response[3] & 0xF0) | (rcode & 0x0F));

        return response;
    }

    private static byte[] BuildRcodeResponseWithEde(byte[] rawRequest, byte rcode, ushort edeCode)
    {
        byte[] baseResponse = BuildRcodeResponse(rawRequest, rcode);
        if (baseResponse.Length < 12)
        {
            return baseResponse;
        }

        return AppendEdeOptRecord(baseResponse, edeCode);
    }

    private static byte[] AppendEdeOptRecord(byte[] baseResponse, ushort edeCode)
    {
        ReadOnlySpan<byte> optRecordSpan = stackalloc byte[]
        {
            0x00,
            0x00, 0x29,
            0x10, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x06,
            (byte)(OptionCodeEdnsError >> 8), (byte)(OptionCodeEdnsError & 0xFF),
            0x00, 0x02,
            (byte)(edeCode >> 8), (byte)(edeCode & 0xFF)
        };

        byte[] finalResponse = new byte[baseResponse.Length + optRecordSpan.Length];
        Array.Copy(baseResponse, finalResponse, baseResponse.Length);
        optRecordSpan.CopyTo(finalResponse.AsSpan(baseResponse.Length));

        ushort arCount = BinaryPrimitives.ReadUInt16BigEndian(finalResponse.AsSpan(10, 2));
        arCount++;
        BinaryPrimitives.WriteUInt16BigEndian(finalResponse.AsSpan(10, 2), arCount);

        return finalResponse;
    }

    private byte[] BuildZeroIpResponse(byte[] rawRequest)
    {
        var msg = DnsMessage.TryParse(rawRequest);
        if (msg == null)
        {
            return BuildRcodeResponseWithEde(rawRequest, 3, EdeCodeBlocked);
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

        return BuildRcodeResponseWithEde(rawRequest, 3, EdeCodeBlocked);
    }

    private byte[] BuildCustomIpResponse(byte[] rawRequest)
    {
        var msg = DnsMessage.TryParse(rawRequest);
        if (msg == null)
        {
            return BuildRcodeResponseWithEde(rawRequest, 3, EdeCodeBlocked);
        }

        string customIp = _options.BlockResponse?.StaticIp ?? "0.0.0.0";
        if (!IPAddress.TryParse(customIp, out var ip))
        {
            ip = IPAddress.Any;
        }

        string qType = msg.QuestionType;

        if ((string.Equals(qType, "A", StringComparison.OrdinalIgnoreCase) || qType == "1") &&
            ip.AddressFamily == AddressFamily.InterNetwork)
        {
            return BuildIpAnswer(rawRequest, msg, ip);
        }

        if ((string.Equals(qType, "AAAA", StringComparison.OrdinalIgnoreCase) || qType == "28") &&
            ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return BuildIpAnswer(rawRequest, msg, ip);
        }

        return BuildRcodeResponseWithEde(rawRequest, 3, EdeCodeBlocked);
    }

    private static byte[] BuildIpAnswer(byte[] rawRequest, DnsMessage msg, IPAddress ip)
    {
        byte[] ipBytes = ip.GetAddressBytes();
        int answerLength = 2 + 2 + 2 + 4 + 2 + ipBytes.Length;
        byte[] response = new byte[rawRequest.Length + answerLength];

        Array.Copy(rawRequest, response, rawRequest.Length);

        response[2] |= 0x80;
        response[3] |= 0x80;
        BinaryPrimitives.WriteUInt16BigEndian(response.AsSpan(6, 2), 1);

        int offset = rawRequest.Length;

        response[offset++] = 0xC0;
        response[offset++] = 0x0C;

        ushort typeCode = string.Equals(msg.QuestionType, "AAAA", StringComparison.OrdinalIgnoreCase) || msg.QuestionType == "28"
            ? (ushort)28
            : (ushort)1;

        BinaryPrimitives.WriteUInt16BigEndian(response.AsSpan(offset, 2), typeCode);
        offset += 2;

        BinaryPrimitives.WriteUInt16BigEndian(response.AsSpan(offset, 2), 1);
        offset += 2;

        BinaryPrimitives.WriteUInt32BigEndian(response.AsSpan(offset, 4), 60);
        offset += 4;

        BinaryPrimitives.WriteUInt16BigEndian(response.AsSpan(offset, 2), (ushort)ipBytes.Length);
        offset += 2;

        ipBytes.CopyTo(response, offset);

        return response;
    }
}
