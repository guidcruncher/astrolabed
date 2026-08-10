using System;
using System.Collections.Generic;
using System.Net;

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
}
