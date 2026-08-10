using System;
using System.Buffers.Binary;
using System.Linq;

using Astrolabed.Dns.Core;

namespace Astrolabed.Utils;

public static class TtlExtractor
{
    public static int ExtractTtl(byte[] response)
    {
        if (response == null || response.Length < 12)
        {
            return 0;
        }

        var message = DnsMessage.TryParse(response);
        if (message == null)
        {
            return 0;
        }

        // 1. Positive answers: return minimum non-zero answer TTL
        if (message.Answers.Count > 0)
        {
            int minTtl = int.MaxValue;
            foreach (var answer in message.Answers)
            {
                if (answer.Ttl > 0 && answer.Ttl < minTtl)
                {
                    minTtl = answer.Ttl;
                }
            }
            return minTtl == int.MaxValue ? 0 : minTtl;
        }

        // 2. Negative caching (RFC 2308): RCODE 3 (NXDOMAIN) or RCODE 0 with 0 answers (NODATA)
        int rcode = response[3] & 0x0F;
        if (rcode == 3 || (rcode == 0 && message.Answers.Count == 0))
        {
            var soaRecord = message.Authorities.FirstOrDefault(a => a.Type == DnsType.SOA);
            if (soaRecord != null)
            {
                int soaRecordTtl = soaRecord.Ttl;
                int soaMinimumField = ExtractSoaMinimumField(soaRecord.RData);

                if (soaMinimumField > 0)
                {
                    // RFC 2308: Negative TTL = min(SOA.TTL, SOA.MINIMUM)
                    return Math.Min(soaRecordTtl, soaMinimumField);
                }

                return Math.Max(0, soaRecordTtl);
            }
        }

        return 0;
    }

    private static int ExtractSoaMinimumField(byte[] rdata)
    {
        if (rdata == null || rdata.Length < 20)
        {
            return 0;
        }

        try
        {
            // MINIMUM TTL is the 32-bit integer located at the final 4 bytes of SOA RDATA
            int offset = rdata.Length - 4;
            uint minTtl = BinaryPrimitives.ReadUInt32BigEndian(rdata.AsSpan(offset, 4));
            return minTtl > int.MaxValue ? int.MaxValue : (int)minTtl;
        }
        catch
        {
            return 0;
        }
    }
}
