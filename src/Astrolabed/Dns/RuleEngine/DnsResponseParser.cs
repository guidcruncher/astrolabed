using System;
using System.Collections.Generic;
using System.Text;

using Astrolabed.Dns.Core;

namespace Astrolabed.Dns.RuleEngine;

public static class DnsResponseParser
{
    public static bool TryParse(byte[] buffer, string domain, ushort queryType, TimeSpan remainingTtl, out DnsResponse? response)
    {
        response = null;
        if (buffer == null || buffer.Length < 12) return false;

        ushort txId = (ushort)((buffer[0] << 8) | buffer[1]);
        byte flags1 = buffer[2];
        byte flags2 = buffer[3];

        bool isResponse = (flags1 & 0x80) != 0;
        byte opCodeVal = (byte)((flags1 >> 3) & 0x0F);
        bool aa = (flags1 & 0x04) != 0;
        bool tc = (flags1 & 0x02) != 0;
        bool rd = (flags1 & 0x01) != 0;
        bool ra = (flags2 & 0x80) != 0;
        bool ad = (flags2 & 0x20) != 0;
        bool cd = (flags2 & 0x10) != 0;
        byte rcodeVal = (byte)(flags2 & 0x0F);

        ushort qdCount = (ushort)((buffer[4] << 8) | buffer[5]);
        ushort anCount = (ushort)((buffer[6] << 8) | buffer[7]);
        ushort nsCount = (ushort)((buffer[8] << 8) | buffer[9]);
        ushort arCount = (ushort)((buffer[10] << 8) | buffer[11]);

        int offset = 12;

        for (int i = 0; i < qdCount; i++)
        {
            if (!TrySkipDomainName(buffer, ref offset)) return false;
            offset += 4;
            if (offset > buffer.Length) return false;
        }

        var answers = ReadResourceRecords(buffer, ref offset, anCount, out _);
        var authorities = ReadResourceRecords(buffer, ref offset, nsCount, out _);
        var additionals = ReadResourceRecords(buffer, ref offset, arCount, out var extendedError);

        var header = new DnsHeader
        {
            TransactionId = txId,
            IsResponse = isResponse,
            OpCode = FormatOpCode(opCodeVal),
            AuthoritativeAnswer = aa,
            Truncated = tc,
            RecursionDesired = rd,
            RecursionAvailable = ra,
            AuthenticData = ad,
            CheckingDisabled = cd,
            QuestionCount = qdCount,
            AnswerCount = anCount,
            NameServerCount = nsCount,
            AdditionalCount = arCount,
            ExtendedError = extendedError
        };

        response = new DnsResponse
        {
            Success = rcodeVal == 0,
            Server = "Cache",
            QueryName = domain,
            QueryType = FormatType(queryType),
            ResponseCode = FormatRCode(rcodeVal),
            Elapsed = remainingTtl,
            Header = header,
            Answers = answers,
            Authorities = authorities,
            Additionals = additionals,
            ExtendedError = extendedError,
            ErrorMessage = rcodeVal == 0 ? null : $"DNS Response Code: {FormatRCode(rcodeVal)}"
        };

        return true;
    }

    public static bool TryExtractEdeOption(ReadOnlySpan<byte> buffer, out ReadOnlySpan<byte> edeOptionSpan)
    {
        edeOptionSpan = default;
        if (buffer.Length < 12) return false;

        ushort qdCount = (ushort)((buffer[4] << 8) | buffer[5]);
        ushort anCount = (ushort)((buffer[6] << 8) | buffer[7]);
        ushort nsCount = (ushort)((buffer[8] << 8) | buffer[9]);
        ushort arCount = (ushort)((buffer[10] << 8) | buffer[11]);

        int offset = 12;

        for (int i = 0; i < qdCount; i++)
        {
            if (!TrySkipDomainNameSpan(buffer, ref offset)) return false;
            offset += 4;
            if (offset > buffer.Length) return false;
        }

        if (!SkipResourceRecords(buffer, ref offset, anCount)) return false;
        if (!SkipResourceRecords(buffer, ref offset, nsCount)) return false;

        for (int i = 0; i < arCount; i++)
        {
            if (!TrySkipDomainNameSpan(buffer, ref offset)) break;
            if (offset + 10 > buffer.Length) break;

            ushort type = (ushort)((buffer[offset] << 8) | buffer[offset + 1]);
            ushort rdLength = (ushort)((buffer[offset + 8] << 8) | buffer[offset + 9]);

            offset += 10;
            if (offset + rdLength > buffer.Length) break;

            if (type == 41) // OPT Record (EDNS)
            {
                int current = offset;
                int end = offset + rdLength;

                while (current + 4 <= end)
                {
                    ushort optionCode = (ushort)((buffer[current] << 8) | buffer[current + 1]);
                    ushort optionLength = (ushort)((buffer[current + 2] << 8) | buffer[current + 3]);

                    if (current + 4 + optionLength > end) break;

                    if (optionCode == 15) // EDNS Option 15: Extended DNS Error
                    {
                        edeOptionSpan = buffer.Slice(current, 4 + optionLength);
                        return true;
                    }

                    current += 4 + optionLength;
                }
            }

            offset += rdLength;
        }

        return false;
    }

    private static bool SkipResourceRecords(ReadOnlySpan<byte> buffer, ref int offset, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (!TrySkipDomainNameSpan(buffer, ref offset)) return false;
            if (offset + 10 > buffer.Length) return false;

            ushort rdLength = (ushort)((buffer[offset + 8] << 8) | buffer[offset + 9]);
            offset += 10 + rdLength;
            if (offset > buffer.Length) return false;
        }

        return true;
    }

    private static List<DnsResource> ReadResourceRecords(byte[] buffer, ref int offset, int count, out DnsExtendedError? extendedError)
    {
        extendedError = null;
        var records = new List<DnsResource>();

        for (int i = 0; i < count; i++)
        {
            if (!TryReadDomainName(buffer, ref offset, out var name)) break;
            if (offset + 10 > buffer.Length) break;

            ushort type = (ushort)((buffer[offset] << 8) | buffer[offset + 1]);
            ushort qclass = (ushort)((buffer[offset + 2] << 8) | buffer[offset + 3]);
            uint ttl = (uint)((buffer[offset + 4] << 24) | (buffer[offset + 5] << 16) | (buffer[offset + 6] << 8) | buffer[offset + 7]);
            ushort rdLength = (ushort)((buffer[offset + 8] << 8) | buffer[offset + 9]);

            offset += 10;
            if (offset + rdLength > buffer.Length) break;

            if (type == 41) // OPT Record (EDNS)
            {
                if (TryParseExtendedDnsError(buffer, offset, rdLength, out var ede))
                {
                    extendedError = ede;
                }
            }

            string data = FormatRecordData(type, qclass, ttl, buffer, offset, rdLength);
            offset += rdLength;

            records.Add(new DnsResource
            {
                Name = name,
                Type = FormatType(type),
                Class = type == 41 ? "NONE" : FormatClass(qclass),
                TimeToLive = ttl,
                Data = data
            });
        }

        return records;
    }

    private static bool TryParseExtendedDnsError(byte[] buffer, int offset, int rdLength, out DnsExtendedError? extendedError)
    {
        extendedError = null;
        int current = offset;
        int end = offset + rdLength;

        while (current + 4 <= end)
        {
            ushort optionCode = (ushort)((buffer[current] << 8) | buffer[current + 1]);
            ushort optionLength = (ushort)((buffer[current + 2] << 8) | buffer[current + 3]);
            current += 4;

            if (current + optionLength > end) break;

            if (optionCode == 15 && optionLength >= 2) // EDNS Option 15: Extended DNS Error
            {
                ushort edeCode = (ushort)((buffer[current] << 8) | buffer[current + 1]);
                string? extraText = null;

                if (optionLength > 2)
                {
                    extraText = Encoding.UTF8.GetString(buffer, current + 2, optionLength - 2).TrimEnd('\0');
                }

                extendedError = new DnsExtendedError
                {
                    Code = edeCode,
                    Name = FormatEdeCode(edeCode),
                    ExtraText = string.IsNullOrWhiteSpace(extraText) ? null : extraText
                };

                return true;
            }

            current += optionLength;
        }

        return false;
    }

    private static bool TryReadDomainName(byte[] buffer, ref int offset, out string domain)
    {
        domain = string.Empty;
        int pointerJumps = 0;
        int currentOffset = offset;
        bool jumped = false;
        var sb = new StringBuilder();

        while (currentOffset < buffer.Length)
        {
            byte len = buffer[currentOffset];
            if (len == 0)
            {
                if (!jumped) offset = currentOffset + 1;
                domain = sb.Length == 0 ? "." : sb.ToString().TrimEnd('.');
                return true;
            }

            if ((len & 0xC0) == 0xC0)
            {
                if (currentOffset + 1 >= buffer.Length) return false;
                if (!jumped) offset = currentOffset + 2;

                currentOffset = ((len & 0x3F) << 8) | buffer[currentOffset + 1];
                jumped = true;
                if (++pointerJumps > 10) return false;
                continue;
            }

            currentOffset++;
            if (currentOffset + len > buffer.Length) return false;

            sb.Append(Encoding.ASCII.GetString(buffer, currentOffset, len)).Append('.');
            currentOffset += len;
        }

        return false;
    }

    private static bool TrySkipDomainName(byte[] buffer, ref int offset)
    {
        return TrySkipDomainNameSpan(buffer, ref offset);
    }

    private static bool TrySkipDomainNameSpan(ReadOnlySpan<byte> buffer, ref int offset)
    {
        while (offset < buffer.Length)
        {
            byte len = buffer[offset];
            if (len == 0)
            {
                offset++;
                return true;
            }
            if ((len & 0xC0) == 0xC0)
            {
                offset += 2;
                return true;
            }
            offset += len + 1;
        }
        return false;
    }

    private static string FormatRecordData(ushort type, ushort qclass, uint ttl, byte[] buffer, int offset, int length)
    {
        try
        {
            switch (type)
            {
                case 1 when length == 4:
                    return new System.Net.IPAddress(buffer.AsSpan(offset, 4)).ToString();

                case 28 when length == 16:
                    return new System.Net.IPAddress(buffer.AsSpan(offset, 16)).ToString();

                case 2:
                case 5:
                case 12:
                case 39:
                    {
                        int ptr = offset;
                        if (TryReadDomainName(buffer, ref ptr, out var domain)) return domain;
                        break;
                    }

                case 6:
                    {
                        int ptr = offset;
                        if (TryReadDomainName(buffer, ref ptr, out var mname) &&
                            TryReadDomainName(buffer, ref ptr, out var rname) &&
                            ptr + 20 <= offset + length)
                        {
                            uint serial = (uint)((buffer[ptr] << 24) | (buffer[ptr + 1] << 16) | (buffer[ptr + 2] << 8) | buffer[ptr + 3]);
                            int refresh = (buffer[ptr + 4] << 24) | (buffer[ptr + 5] << 16) | (buffer[ptr + 6] << 8) | buffer[ptr + 7];
                            int retry = (buffer[ptr + 8] << 24) | (buffer[ptr + 9] << 16) | (buffer[ptr + 10] << 8) | buffer[ptr + 11];
                            int expire = (buffer[ptr + 12] << 24) | (buffer[ptr + 13] << 16) | (buffer[ptr + 14] << 8) | buffer[ptr + 15];
                            uint minimum = (uint)((buffer[ptr + 16] << 24) | (buffer[ptr + 17] << 16) | (buffer[ptr + 18] << 8) | buffer[ptr + 19]);

                            return $"{mname} {rname} {serial} {refresh} {retry} {expire} {minimum}";
                        }
                        break;
                    }

                case 15:
                    {
                        if (length > 2)
                        {
                            ushort preference = (ushort)((buffer[offset] << 8) | buffer[offset + 1]);
                            int ptr = offset + 2;
                            if (TryReadDomainName(buffer, ref ptr, out var exchange))
                            {
                                return $"{preference} {exchange}";
                            }
                        }
                        break;
                    }

                case 16:
                case 99:
                    {
                        var sb = new StringBuilder();
                        int curr = offset;
                        int end = offset + length;
                        while (curr < end)
                        {
                            byte strLen = buffer[curr++];
                            if (curr + strLen > end) break;
                            sb.Append('"').Append(Encoding.UTF8.GetString(buffer, curr, strLen)).Append("\" ");
                            curr += strLen;
                        }
                        return sb.ToString().TrimEnd();
                    }

                case 33:
                    {
                        if (length >= 6)
                        {
                            ushort priority = (ushort)((buffer[offset] << 8) | buffer[offset + 1]);
                            ushort weight = (ushort)((buffer[offset + 2] << 8) | buffer[offset + 3]);
                            ushort port = (ushort)((buffer[offset + 4] << 8) | buffer[offset + 5]);
                            int ptr = offset + 6;
                            if (TryReadDomainName(buffer, ref ptr, out var target))
                            {
                                return $"{priority} {weight} {port} {target}";
                            }
                        }
                        break;
                    }

                case 35:
                    {
                        if (length >= 7)
                        {
                            ushort order = (ushort)((buffer[offset] << 8) | buffer[offset + 1]);
                            ushort preference = (ushort)((buffer[offset + 2] << 8) | buffer[offset + 3]);
                            int ptr = offset + 4;

                            byte flagsLen = buffer[ptr++];
                            string flags = Encoding.ASCII.GetString(buffer, ptr, flagsLen);
                            ptr += flagsLen;

                            byte servicesLen = buffer[ptr++];
                            string services = Encoding.ASCII.GetString(buffer, ptr, servicesLen);
                            ptr += servicesLen;

                            byte regexpLen = buffer[ptr++];
                            string regexp = Encoding.ASCII.GetString(buffer, ptr, regexpLen);
                            ptr += regexpLen;

                            if (TryReadDomainName(buffer, ref ptr, out var replacement))
                            {
                                return $"{order} {preference} \"{flags}\" \"{services}\" \"{regexp}\" {replacement}";
                            }
                        }
                        break;
                    }

                case 257:
                    {
                        if (length >= 2)
                        {
                            byte flags = buffer[offset];
                            byte tagLen = buffer[offset + 1];
                            if (offset + 2 + tagLen <= offset + length)
                            {
                                string tag = Encoding.ASCII.GetString(buffer, offset + 2, tagLen);
                                string val = Encoding.ASCII.GetString(buffer, offset + 2 + tagLen, length - 2 - tagLen);
                                return $"{flags} {tag} \"{val}\"";
                            }
                        }
                        break;
                    }

                case 41:
                    {
                        ushort udpSize = qclass;
                        byte extRCode = (byte)((ttl >> 24) & 0xFF);
                        byte version = (byte)((ttl >> 16) & 0xFF);
                        ushort flags = (ushort)(ttl & 0xFFFF);
                        return $"UDPSize: {udpSize}, ExtRCode: {extRCode}, Version: {version}, Flags: {flags}, OptionsLength: {length}";
                    }
            }
        }
        catch
        {
            // Fall back to hex representation on malformed data
        }

        return Convert.ToHexString(buffer, offset, length);
    }

    private static string FormatEdeCode(ushort edeCode) => edeCode switch
    {
        0 => "Other Error",
        1 => "Unsupported DNSKEY Algorithm",
        2 => "Unsupported DS Digest Type",
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
        21 => "Not Supported",
        22 => "No Reachable Authority",
        23 => "Network Error",
        24 => "Invalid Data",
        25 => "Signature Expired Before Inception",
        26 => "Too Many Records",
        27 => "Unsupported AAAA Guard",
        _ => $"EDE_{edeCode}"
    };

    private static string FormatOpCode(byte opCode) => opCode switch
    {
        0 => "QUERY",
        1 => "IQUERY",
        2 => "STATUS",
        4 => "NOTIFY",
        5 => "UPDATE",
        _ => $"OPCODE_{opCode}"
    };

    private static string FormatRCode(byte rcode) => rcode switch
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
        16 => "BADVERS",
        _ => $"RCODE_{rcode}"
    };

    private static string FormatType(ushort type) => type switch
    {
        1 => "A",
        2 => "NS",
        5 => "CNAME",
        6 => "SOA",
        12 => "PTR",
        13 => "HINFO",
        15 => "MX",
        16 => "TXT",
        17 => "RP",
        18 => "AFSDB",
        24 => "SIG",
        25 => "KEY",
        28 => "AAAA",
        29 => "LOC",
        33 => "SRV",
        35 => "NAPTR",
        36 => "KX",
        37 => "CERT",
        39 => "DNAME",
        41 => "OPT",
        43 => "DS",
        46 => "RRSIG",
        47 => "NSEC",
        48 => "DNSKEY",
        50 => "NSEC3",
        51 => "NSEC3PARAM",
        52 => "TLSA",
        53 => "SMIMEA",
        55 => "HIP",
        59 => "CDS",
        60 => "CDNSKEY",
        61 => "OPENPGPKEY",
        62 => "CSYNC",
        63 => "ZONEMD",
        64 => "SVCB",
        65 => "HTTPS",
        99 => "SPF",
        249 => "TKEY",
        250 => "TSIG",
        251 => "IXFR",
        252 => "AXFR",
        255 => "ANY",
        256 => "URI",
        257 => "CAA",
        _ => $"TYPE_{type}"
    };

    private static string FormatClass(ushort qclass) => qclass switch
    {
        1 => "IN",
        2 => "CS",
        3 => "CH",
        4 => "HS",
        254 => "NONE",
        255 => "ANY",
        _ => $"CLASS_{qclass}"
    };
}
