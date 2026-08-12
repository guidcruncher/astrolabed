using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace Astrolabed.Dns.Core;

public sealed class DnsMessage
{
    public ushort Id { get; set; }
    public bool IsResponse { get; set; }

    public List<DnsQuestion> Questions { get; } = new();
    public List<DnsResourceRecord> Answers { get; } = new();
    public List<DnsResourceRecord> Authorities { get; } = new();
    public List<DnsResourceRecord> Additionals { get; } = new();

    // Metrics fields
    public string ResponseCode { get; set; } = "NOERROR";

    /// <summary>
    /// Safely extracts the first valid A or AAAA IP address from the answer section.
    /// Returns null for non-IP record types (e.g., MX, CNAME, TXT).
    /// </summary>
    public IPAddress? AnswerAddress
    {
        get
        {
            foreach (var answer in Answers)
            {
                if (answer.Type == DnsType.A && answer.RData.Length == 4)
                {
                    return new IPAddress(answer.RData);
                }
                if (answer.Type == DnsType.AAAA && answer.RData.Length == 16)
                {
                    return new IPAddress(answer.RData);
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Extracts the host name string from the first valid PTR record in the answer section.
    /// Supports parsing compressed domain names embedded in RDATA.
    /// </summary>
    public string? AnswerHostName
    {
        get
        {
            foreach (var answer in Answers)
            {
                if (answer.Type == DnsType.PTR && answer.RData.Length > 0)
                {
                    return ReadDomainName(answer.RData, 0, answer.FullMessageBuffer);
                }
            }

            return null;
        }
    }

    // Convenience properties for DNS server + metrics
    public string QuestionName
        => Questions.Count > 0 ? Questions[0].Name : string.Empty;

    public string QuestionType
        => Questions.Count > 0 ? Questions[0].Type.ToString() : string.Empty;

    public int GetMinTtl()
    {
        if (Answers.Count == 0)
        {
            return 60;
        }

        int minTtl = int.MaxValue;
        foreach (var answer in Answers)
        {
            if (answer.Ttl < minTtl)
            {
                minTtl = answer.Ttl;
            }
        }

        return minTtl == int.MaxValue ? 60 : minTtl;
    }

    public static DnsMessage? TryParse(byte[] buffer)
    {
        if (buffer == null || buffer.Length < 12)
        {
            return null;
        }

        try
        {
            return DnsParser.Parse(buffer);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Constructs a standard raw DNS wire-format query buffer for a PTR record request.
    /// </summary>
    /// <param name="ptrQueryName">The reverse domain name (e.g., "50.1.168.192.in-addr.arpa").</param>
    /// <returns>A byte array containing the RFC-compliant DNS query packet.</returns>
    public static byte[] CreatePtrQuery(string ptrQueryName)
    {
        ushort queryId = (ushort)Random.Shared.Next(1, 65535);
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Header Section (12 bytes)
        writer.Write(IPAddress.HostToNetworkOrder((short)queryId)); // Transaction ID
        writer.Write((byte)0x01); // Flags byte 1: Standard Query, Recursion Desired (RD = 1)
        writer.Write((byte)0x00); // Flags byte 2
        writer.Write(IPAddress.HostToNetworkOrder((short)1)); // QDCOUNT = 1
        writer.Write((short)0); // ANCOUNT = 0
        writer.Write((short)0); // NSCOUNT = 0
        writer.Write((short)0); // ARCOUNT = 0

        // Question Section
        string[] labels = ptrQueryName.Split('.', StringSplitOptions.RemoveEmptyEntries);
        foreach (string label in labels)
        {
            byte[] labelBytes = Encoding.ASCII.GetBytes(label);
            writer.Write((byte)labelBytes.Length);
            writer.Write(labelBytes);
        }
        writer.Write((byte)0); // End of QNAME labels

        writer.Write(IPAddress.HostToNetworkOrder((short)DnsType.PTR)); // QTYPE = PTR (12)
        writer.Write(IPAddress.HostToNetworkOrder((short)1)); // QCLASS = IN (1)

        return ms.ToArray();
    }

    private static string? ReadDomainName(byte[] rdata, int offset, byte[]? fullMessageBuffer)
    {
        try
        {
            var sb = new StringBuilder();
            byte[] source = fullMessageBuffer ?? rdata;
            int current = fullMessageBuffer != null ? FindRdataOffset(fullMessageBuffer, rdata) + offset : offset;

            if (current < 0 || current >= source.Length)
            {
                source = rdata;
                current = offset;
            }

            int jumps = 0;
            const int maxJumps = 5;

            while (current < source.Length)
            {
                byte length = source[current];
                if (length == 0)
                {
                    break;
                }

                // Check for DNS pointer compression (0xC0 flag)
                if ((length & 0xC0) == 0xC0)
                {
                    if (current + 1 >= source.Length)
                    {
                        break;
                    }

                    if (jumps++ > maxJumps)
                    {
                        break;
                    }

                    int pointerOffset = ((length & 0x3F) << 8) | source[current + 1];
                    current = pointerOffset;
                    continue;
                }

                current++;
                if (current + length > source.Length)
                {
                    break;
                }

                if (sb.Length > 0)
                {
                    sb.Append('.');
                }

                sb.Append(Encoding.ASCII.GetString(source, current, length));
                current += length;
            }

            return sb.Length > 0 ? sb.ToString() : null;
        }
        catch
        {
            return null;
        }
    }

    private static int FindRdataOffset(byte[] fullBuffer, byte[] rdata)
    {
        if (rdata.Length == 0 || fullBuffer.Length < rdata.Length)
        {
            return -1;
        }

        return MemoryExtensions.IndexOf(fullBuffer.AsSpan(), rdata.AsSpan());
    }
}

public sealed class DnsQuestion
{
    public string Name { get; set; } = string.Empty;
    public DnsType Type { get; set; }
    public ushort Class { get; set; }
}

public sealed class DnsResourceRecord
{
    public string Name { get; set; } = string.Empty;
    public DnsType Type { get; set; }
    public ushort Class { get; set; }
    public int Ttl { get; set; }
    public byte[] RData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Optional reference to the complete DNS packet for compressed domain pointer resolution.
    /// </summary>
    public byte[]? FullMessageBuffer { get; set; }
}
