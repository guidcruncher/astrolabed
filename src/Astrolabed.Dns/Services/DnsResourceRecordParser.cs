namespace Astrolabed.Dns.Services;

using Astrolabed.Dns.Models;

using System.Net;
using System.Text;

/// <summary>
/// Provides utility methods to parse and format <see cref="DnsResourceRecord"/> objects into string representations
/// suitable for storage in <c>AnswerData</c>.
/// </summary>
public static class DnsResourceRecordParser
{
    /// <summary>
    /// Parses an array or collection of <see cref="DnsResourceRecord"/> instances into a flat string list for storage.
    /// </summary>
    /// <param name="records">The collection of DNS resource records to parse.</param>
    /// <returns>An <see cref="IReadOnlyList{T}"/> of human-readable record values, or <see langword="null"/> if empty.</returns>
    public static IReadOnlyList<string>? ToAnswerData(this IEnumerable<DnsResourceRecord>? records)
    {
        if (records is null)
        {
            return null;
        }

        var results = new List<string>();

        foreach (var record in records)
        {
            if (record is null)
            {
                continue;
            }

            string parsedValue = FormatRecordData(record);
            if (!string.IsNullOrWhiteSpace(parsedValue))
            {
                results.Add(parsedValue);
            }
        }

        return results.Count > 0 ? results.AsReadOnly() : null;
    }

    /// <summary>
    /// Formats an individual <see cref="DnsResourceRecord"/> into a standardized string representation based on its type.
    /// </summary>
    /// <param name="record">The record to format.</param>
    /// <returns>A formatted representation of the record payload.</returns>
    private static string FormatRecordData(DnsResourceRecord record)
    {
        // 1. If ParsedIp is present (A or AAAA records), use it directly
        if (record.ParsedIp is not null)
        {
            return record.ParsedIp.ToString();
        }

        // 2. Fall back to parsing Data bytes based on common DNS types
        if (record.Data is null || record.Data.Length == 0)
        {
            return string.Empty;
        }

        return record.Type switch
        {
            DnsType.A when record.Data.Length == 4 => new IPAddress(record.Data).ToString(),
            DnsType.AAAA when record.Data.Length == 16 => new IPAddress(record.Data).ToString(),
            DnsType.CNAME or DnsType.PTR or DnsType.NS => DecodeDnsName(record.Data),
            DnsType.TXT => Encoding.UTF8.GetString(record.Data).Trim('\0', ' '),
            _ => FormatGenericRecord(record)
        };
    }

    /// <summary>
    /// Decodes a uncompressed length-prefixed DNS domain name sequence into standard dot-separated string format.
    /// </summary>
    private static string DecodeDnsName(byte[] data)
    {
        var sb = new StringBuilder();
        int index = 0;

        while (index < data.Length)
        {
            byte length = data[index++];
            if (length == 0)
            {
                break;
            }

            if (index + length > data.Length)
            {
                // Edge case: malformed wire format fallback
                return Encoding.ASCII.GetString(data).Trim('\0');
            }

            if (sb.Length > 0)
            {
                sb.Append('.');
            }

            sb.Append(Encoding.ASCII.GetString(data, index, length));
            index += length;
        }

        return sb.Length > 0 ? sb.ToString() : Encoding.ASCII.GetString(data).Trim('\0');
    }

    /// <summary>
    /// Fallback string representation for record types where structural extraction is not explicitly handled.
    /// </summary>
    private static string FormatGenericRecord(DnsResourceRecord record)
    {
        // Fall back to ASCII attempt if printability is likely, else Hex string
        bool isPrintableAscii = Array.TrueForAll(record.Data, b => b is >= 32 and <= 126);

        if (isPrintableAscii)
        {
            return Encoding.ASCII.GetString(record.Data);
        }

        return $"0x{Convert.ToHexString(record.Data)}";
    }
}

