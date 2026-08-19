using System;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

using Astrolabed.Dns.Core;

using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.RuleEngine;

internal sealed class BlockResponseBuilder
{
    private readonly DnsForwarderOptions _options;

    // EDNS / EDE Constants (RFC 6891 & RFC 8914)
    private const ushort OptionCodeEdnsError = 15; // OPTION-CODE 15: Extended DNS Error
    private const ushort EdeCodeBlocked = 15;      // EDE 15: Blocked (Administrative policy or blocklist)
    private const ushort EdeCodeFiltered = 17;     // EDE 17: Filtered (User-configured filtering rules)
    private const ushort EdeCodeProhibited = 18;   // EDE 18: Prohibited (Authoritative server policy)

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
            "nxdomain" => BuildRcodeResponseWithEde(rawRequest, 3, EdeCodeBlocked),  // NXDOMAIN + EDE 15 (Blocked)
            "refused" => BuildRcodeResponseWithEde(rawRequest, 5, EdeCodeBlocked),   // REFUSED + EDE 15 (Blocked)
            "filtered" => BuildRcodeResponseWithEde(rawRequest, 5, EdeCodeFiltered),  // REFUSED + EDE 17 (Filtered)
            "nodata" => BuildNoDataResponseWithEde(rawRequest, EdeCodeBlocked),       // NOERROR + ANCOUNT 0 + EDE 15 (Blocked)
            "servfail" => BuildServfail(rawRequest ?? Array.Empty<byte>()),
            "zeroip" => BuildZeroIpResponse(rawRequest),
            "customip" => BuildCustomIpResponse(rawRequest),
            _ => BuildRcodeResponseWithEde(rawRequest, 3, EdeCodeBlocked)
        };
    }

    public static byte[] BuildServfail(byte[] rawRequest)
    {
        return BuildRcodeResponse(rawRequest, 2); // SERVFAIL without EDE block metadata
    }

    /// <summary>
    /// Builds a NODATA (NOERROR with 0 Answer records) response and appends an EDE OPT record.
    /// </summary>
    private static byte[] BuildNoDataResponseWithEde(byte[] rawRequest, ushort edeCode)
    {
        // NOERROR = RCODE 0
        byte[] baseResponse = BuildRcodeResponse(rawRequest, 0);
        if (baseResponse.Length < 12)
        {
            return baseResponse;
        }

        // Explicitly set Answer Count (ANCOUNT) at header offset 4 to 0
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

        // Set QR bit = 1 (Response)
        response[2] |= 0x80;

        // Set RCODE in header lower 4 bits of byte 3
        response[3] = (byte)((response[3] & 0xF0) | (rcode & 0x0F));

        return response;
    }

    /// <summary>
    /// Builds a DNS response with a specified RCODE and appends an OPT record containing an Extended DNS Error (EDE).
    /// </summary>
    private static byte[] BuildRcodeResponseWithEde(byte[] rawRequest, byte rcode, ushort edeCode)
    {
        byte[] baseResponse = BuildRcodeResponse(rawRequest, rcode);
        if (baseResponse.Length < 12)
        {
            return baseResponse;
        }

        return AppendEdeOptRecord(baseResponse, edeCode);
    }

    /// <summary>
    /// Appends an OPT pseudo-record containing an EDE Option Code to a raw DNS response buffer.
    /// </summary>
    private static byte[] AppendEdeOptRecord(byte[] baseResponse, ushort edeCode)
    {
        // OPT RR structure containing EDE Option 15/17:
        // Name: Root (0x00) -> 1 byte
        // TYPE: OPT (41) -> 2 bytes
        // UDP Payload Size: 4096 -> 2 bytes
        // Extended RCODE & Flags: 0 -> 4 bytes
        // RDLENGTH: 6 bytes (OptionCode [2] + OptionLen [2] + EdeCode [2]) -> 2 bytes
        // RDATA:
        //   Option Code: 15 (EDE) -> 2 bytes
        //   Option Length: 2 -> 2 bytes
        //   EDE Info Code: edeCode -> 2 bytes
        ReadOnlySpan<byte> optRecordSpan = stackalloc byte[]
        {
            0x00,                   // Root Domain
            0x00, 0x29,             // TYPE 41 (OPT)
            0x10, 0x00,             // UDP payload size (4096)
            0x00, 0x00, 0x00, 0x00, // Higher RCODE + EDNS Version 0 + Flags
            0x00, 0x06,             // RDLENGTH = 6 bytes
            (byte)(OptionCodeEdnsError >> 8), (byte)(OptionCodeEdnsError & 0xFF), // Option Code 15
            0x00, 0x02,             // Option Length 2
            (byte)(edeCode >> 8), (byte)(edeCode & 0xFF)                         // EDE Info Code
        };

        byte[] finalResponse = new byte[baseResponse.Length + optRecordSpan.Length];
        Array.Copy(baseResponse, finalResponse, baseResponse.Length);
        optRecordSpan.CopyTo(finalResponse.AsSpan(baseResponse.Length));

        // Increment ARCOUNT (Additional Records Count) in DNS Header (Offset 10)
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
