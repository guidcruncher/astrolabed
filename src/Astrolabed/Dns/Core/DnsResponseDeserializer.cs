using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net;
using System.Text;

using Astrolabed.Dns.Core;

namespace Astrolabed.Dns.Core;

public static class DnsResponseDeserializer
{
    private const ushort EdnsOptionCodeEde = 15;

    public static DnsResponse Deserialize(
        PooledBuffer buffer,
        string server,
        TimeSpan elapsed,
        string defaultQueryName = "",
        string defaultQueryType = "A")
    {
        ArgumentNullException.ThrowIfNull(buffer);

        ReadOnlySpan<byte> data = buffer.Span;

        if (data.Length < 12)
        {
            return CreateFailureResponse(
                server,
                elapsed,
                defaultQueryName,
                defaultQueryType,
                "Malformed DNS response: Header is too short.");
        }

        try
        {
            ushort transactionId = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(0, 2));
            ushort flags = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(2, 2));
            ushort qdCount = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(4, 2));
            ushort anCount = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(6, 2));
            ushort nsCount = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(8, 2));
            ushort arCount = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(10, 2));

            bool isResponse = (flags & 0x8000) != 0;
            int opCodeValue = (flags >> 11) & 0x0F;
            bool authoritativeAnswer = (flags & 0x0400) != 0;
            bool truncated = (flags & 0x0200) != 0;
            bool recursionDesired = (flags & 0x0100) != 0;
            bool recursionAvailable = (flags & 0x0080) != 0;
            bool authenticData = (flags & 0x0020) != 0;
            bool checkingDisabled = (flags & 0x0010) != 0;
            byte rcodeValue = (byte)(flags & 0x0F);

            string opCode = GetOpCodeString(opCodeValue);
            string responseCode = GetRcodeString(rcodeValue);

            int offset = 12;
            string queryName = defaultQueryName;
            string queryType = defaultQueryType;

            for (int i = 0; i < qdCount; i++)
            {
                string parsedQName = ReadDomainName(data, ref offset);
                ushort qTypeNum = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
                offset += 4; // Skip QTYPE (2) and QCLASS (2)

                if (i == 0)
                {
                    queryName = parsedQName;
                    queryType = GetDnsTypeString(qTypeNum);
                }
            }

            var answers = ReadResourceRecords(data, ref offset, anCount, out _);
            var authorities = ReadResourceRecords(data, ref offset, nsCount, out _);
            var additionals = ReadResourceRecords(data, ref offset, arCount, out DnsExtendedError? ede);

            var header = new DnsHeader
            {
                TransactionId = transactionId,
                IsResponse = isResponse,
                OpCode = opCode,
                AuthoritativeAnswer = authoritativeAnswer,
                Truncated = truncated,
                RecursionDesired = recursionDesired,
                RecursionAvailable = recursionAvailable,
                AuthenticData = authenticData,
                CheckingDisabled = checkingDisabled,
                QuestionCount = qdCount,
                AnswerCount = anCount,
                NameServerCount = nsCount,
                AdditionalCount = arCount,
                ExtendedError = ede
            };

            return new DnsResponse
            {
                Success = true,
                Server = server,
                QueryName = queryName,
                QueryType = queryType,
                ResponseCode = responseCode,
                Elapsed = elapsed,
                Header = header,
                Answers = answers,
                Authorities = authorities,
                Additionals = additionals,
                ExtendedError = ede,
                ErrorMessage = null
            };
        }
        catch (Exception ex)
        {
            return CreateFailureResponse(
                server,
                elapsed,
                defaultQueryName,
                defaultQueryType,
                $"Failed to deserialize DNS packet: {ex.Message}");
        }
    }

    private static List<DnsResource> ReadResourceRecords(
        ReadOnlySpan<byte> data,
        ref int offset,
        ushort count,
        out DnsExtendedError? ede)
    {
        ede = null;
        var records = new List<DnsResource>(count);

        for (int i = 0; i < count; i++)
        {
            string name = ReadDomainName(data, ref offset);
            ushort typeNum = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
            ushort classNum = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset + 2, 2));
            uint ttl = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset + 4, 4));
            ushort rdLength = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset + 8, 2));

            offset += 10;
            int rdataStart = offset;

            if (typeNum == 41) // OPT Record (EDNS0)
            {
                ede = ParseEdnsOptions(data.Slice(rdataStart, rdLength));
            }
            else
            {
                string rdataStr = ParseRdata(data, rdataStart, rdLength, typeNum);
                records.Add(new DnsResource
                {
                    Name = name,
                    Type = GetDnsTypeString(typeNum),
                    Class = GetDnsClassString(classNum),
                    TimeToLive = ttl,
                    Data = rdataStr
                });
            }

            offset = rdataStart + rdLength;
        }

        return records;
    }

    private static string ParseRdata(ReadOnlySpan<byte> data, int rdataOffset, ushort rdLength, ushort type)
    {
        ReadOnlySpan<byte> rdata = data.Slice(rdataOffset, rdLength);

        return type switch
        {
            1 when rdLength == 4 => new IPAddress(rdata).ToString(), // A
            28 when rdLength == 16 => new IPAddress(rdata).ToString(), // AAAA
            2 or 5 or 12 => ReadDomainName(data, ref rdataOffset), // NS, CNAME, PTR
            15 => ParseMxRdata(data, rdataOffset), // MX
            16 => ParseTxtRdata(rdata), // TXT
            6 => ParseSoaRdata(data, rdataOffset), // SOA
            _ => Convert.ToHexString(rdata)
        };
    }

    private static string ParseMxRdata(ReadOnlySpan<byte> data, int offset)
    {
        ushort preference = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
        offset += 2;
        string exchange = ReadDomainName(data, ref offset);
        return $"{preference} {exchange}";
    }

    private static string ParseSoaRdata(ReadOnlySpan<byte> data, int offset)
    {
        string mName = ReadDomainName(data, ref offset);
        string rName = ReadDomainName(data, ref offset);
        uint serial = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        uint refresh = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset + 4, 4));
        uint retry = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset + 8, 4));
        uint expire = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset + 12, 4));
        uint minimum = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset + 16, 4));

        return $"{mName} {rName} {serial} {refresh} {retry} {expire} {minimum}";
    }

    private static string ParseTxtRdata(ReadOnlySpan<byte> rdata)
    {
        var sb = new StringBuilder();
        int offset = 0;

        while (offset < rdata.Length)
        {
            byte len = rdata[offset++];
            if (offset + len > rdata.Length) break;

            if (sb.Length > 0) sb.Append(' ');
            sb.Append('"').Append(Encoding.UTF8.GetString(rdata.Slice(offset, len))).Append('"');
            offset += len;
        }

        return sb.ToString();
    }

    private static DnsExtendedError? ParseEdnsOptions(ReadOnlySpan<byte> optionsSpan)
    {
        int offset = 0;
        while (offset + 4 <= optionsSpan.Length)
        {
            ushort optionCode = BinaryPrimitives.ReadUInt16BigEndian(optionsSpan.Slice(offset, 2));
            ushort optionLength = BinaryPrimitives.ReadUInt16BigEndian(optionsSpan.Slice(offset + 2, 2));
            offset += 4;

            if (offset + optionLength > optionsSpan.Length) break;

            if (optionCode == EdnsOptionCodeEde && optionLength >= 2)
            {
                ushort edeCode = BinaryPrimitives.ReadUInt16BigEndian(optionsSpan.Slice(offset, 2));
                string? extraText = optionLength > 2
                    ? Encoding.UTF8.GetString(optionsSpan.Slice(offset + 2, optionLength - 2)).TrimEnd('\0')
                    : null;

                return new DnsExtendedError
                {
                    Code = edeCode,
                    Name = GetEdeName(edeCode),
                    ExtraText = string.IsNullOrWhiteSpace(extraText) ? null : extraText
                };
            }

            offset += optionLength;
        }

        return null;
    }

    private static string ReadDomainName(ReadOnlySpan<byte> data, ref int offset)
    {
        var sb = new StringBuilder();
        int originalOffset = offset;
        bool jumped = false;
        int jumps = 0;

        while (true)
        {
            if (offset >= data.Length) break;

            byte len = data[offset++];
            if (len == 0) break;

            if ((len & 0xC0) == 0xC0)
            {
                if (offset >= data.Length) break;

                byte b2 = data[offset++];
                int pointer = ((len & 0x3F) << 8) | b2;

                if (!jumped)
                {
                    originalOffset = offset;
                    jumped = true;
                }

                offset = pointer;

                if (++jumps > 10) break; // Circular reference protection
                continue;
            }

            if (offset + len > data.Length) break;

            if (sb.Length > 0) sb.Append('.');
            sb.Append(Encoding.ASCII.GetString(data.Slice(offset, len)));
            offset += len;
        }

        if (jumped)
        {
            offset = originalOffset;
        }

        return sb.Length == 0 ? "." : sb.ToString();
    }

    private static DnsResponse CreateFailureResponse(
        string server,
        TimeSpan elapsed,
        string queryName,
        string queryType,
        string error)
    {
        return new DnsResponse
        {
            Success = false,
            Server = server,
            QueryName = queryName,
            QueryType = queryType,
            ResponseCode = "SERVFAIL",
            Elapsed = elapsed,
            Header = new DnsHeader
            {
                TransactionId = 0,
                IsResponse = true,
                OpCode = "QUERY",
                AuthoritativeAnswer = false,
                Truncated = false,
                RecursionDesired = false,
                RecursionAvailable = false,
                AuthenticData = false,
                CheckingDisabled = false,
                QuestionCount = 0,
                AnswerCount = 0,
                NameServerCount = 0,
                AdditionalCount = 0
            },
            Answers = Array.Empty<DnsResource>(),
            Authorities = Array.Empty<DnsResource>(),
            Additionals = Array.Empty<DnsResource>(),
            ErrorMessage = error
        };
    }

    private static string GetOpCodeString(int opCode) => opCode switch
    {
        0 => "QUERY",
        1 => "IQUERY",
        2 => "STATUS",
        4 => "NOTIFY",
        5 => "UPDATE",
        _ => $"OPCODE_{opCode}"
    };

    private static string GetRcodeString(byte rcode) => rcode switch
    {
        0 => "NOERROR",
        1 => "FORMERR",
        2 => "SERVFAIL",
        3 => "NXDOMAIN",
        4 => "NOTIMP",
        5 => "REFUSED",
        6 => "YXDOMAIN",
        7 => "YXRRSET",
        8 => "NXRRSET",
        9 => "NOTAUTH",
        10 => "NOTZONE",
        _ => $"RCODE_{rcode}"
    };

    private static string GetDnsTypeString(ushort type) => type switch
    {
        1 => "A",
        2 => "NS",
        5 => "CNAME",
        6 => "SOA",
        12 => "PTR",
        15 => "MX",
        16 => "TXT",
        28 => "AAAA",
        33 => "SRV",
        41 => "OPT",
        65 => "HTTPS",
        255 => "ANY",
        _ => $"TYPE{type}"
    };

    private static string GetDnsClassString(ushort dnsClass) => dnsClass switch
    {
        1 => "IN",
        3 => "CH",
        4 => "HS",
        255 => "ANY",
        _ => $"CLASS{dnsClass}"
    };

    private static string GetEdeName(ushort code) => code switch
    {
        0 => "Other Error",
        1 => "Unsupported DNSKEY Algorithm",
        2 => "Unsupported DS Digest Algorithm",
        3 => "Stale Answer",
        4 => "Forged Answer",
        5 => "DNSSEC Indeterminate",
        6 => "DNSSEC Bogus",
        7 => "Signature Expired",
        8 => "Signature Not Yet Valid",
        9 => "DNSKEY Missing",
        10 => "RRSIGs Missing",
        11 => "No Zone Key Bit Set",
        12 => "NSEC Missing",
        13 => "Cached Error",
        14 => "Not Ready",
        15 => "Blocked",
        16 => "Censored",
        17 => "Filtered",
        18 => "Prohibited",
        19 => "Stale NXDOMAIN Answer",
        20 => "Not Authoritative",
        21 => "Not Zone",
        22 => "Revoked DNSKEY",
        23 => "Prohibited Answer",
        24 => "Dnskey Missing",
        _ => $"EDE_{code}"
    };
}
