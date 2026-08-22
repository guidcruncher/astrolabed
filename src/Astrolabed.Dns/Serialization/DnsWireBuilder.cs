// File: src/Astrolabed.Dns/Serialization/DnsWireBuilder.cs
using System.Buffers.Binary;
using System.Text;

using Astrolabed.Dns.Models;

namespace Astrolabed.Dns.Serialization;

public static class DnsWireBuilder
{
    public static byte[] BuildResponse(
        DnsWireMessage request,
        DnsResponseCode responseCode,
        IEnumerable<DnsResourceRecord>? answers = null,
        ExtendedDnsError? ede = null)
    {
        Span<byte> header = stackalloc byte[12];

        // 1. Preserve incoming Transaction ID
        BinaryPrimitives.WriteUInt16BigEndian(header[0..2], request.TransactionId);

        // 2. Response Flags: QR=1 (Response), Opcode=0, AA=1, RD=1, RA=1, RCODE
        ushort flags = 0x8180;
        flags |= (ushort)((byte)responseCode & 0x0F);
        BinaryPrimitives.WriteUInt16BigEndian(header[2..4], flags);

        // 3. Question Count
        BinaryPrimitives.WriteUInt16BigEndian(header[4..6], 1);

        // Calculate Answer Count
        var answerList = answers != null ? new List<DnsResourceRecord>(answers) : [];
        BinaryPrimitives.WriteUInt16BigEndian(header[6..8], (ushort)answerList.Count);

        // Authority Count
        BinaryPrimitives.WriteUInt16BigEndian(header[8..10], 0);

        // Additional Count (1 if EDE present for EDNS0 OPT RR, else 0)
        BinaryPrimitives.WriteUInt16BigEndian(header[10..12], ede != null ? (ushort)1 : (ushort)0);

        var buffer = new List<byte>(256);
        buffer.AddRange(header);

        // DNS domain compression tracking dictionary (suffix -> byte offset in output buffer)
        var compressionMap = new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase);

        // Encode Question Section with compression tracking
        WriteDomainName(buffer, request.QuestionName, compressionMap);

        byte[] typeAndClass = new byte[4];
        BinaryPrimitives.WriteUInt16BigEndian(typeAndClass.AsSpan(0, 2), (ushort)request.QuestionType);
        BinaryPrimitives.WriteUInt16BigEndian(typeAndClass.AsSpan(2, 2), 1); // Class IN
        buffer.AddRange(typeAndClass);

        // Encode Answer RRs
        foreach (var rr in answerList)
        {
            EncodeResourceRecord(buffer, rr, compressionMap);
        }

        // Encode EDNS0 OPT Pseudo-RR if Extended DNS Error is present
        if (ede != null)
        {
            EncodeEdnsOption(buffer, ede);
        }

        return buffer.ToArray();
    }

    public static void EncodeDomainName(byte[] buffer, ref int offset, string domain)
    {
        var tempBuffer = new List<byte>();
        var dummyMap = new Dictionary<string, ushort>();
        WriteDomainName(tempBuffer, domain, dummyMap);

        foreach (byte b in tempBuffer)
        {
            buffer[offset++] = b;
        }
    }

    public static void WriteDomainName(
        List<byte> buffer,
        string domain,
        Dictionary<string, ushort> compressionMap)
    {
        if (string.IsNullOrEmpty(domain) || domain == ".")
        {
            buffer.Add(0);
            return;
        }

        string normalized = domain.TrimEnd('.');
        string[] labels = normalized.Split('.');

        for (int i = 0; i < labels.Length; i++)
        {
            string suffix = string.Join('.', labels, i, labels.Length - i);

            if (compressionMap.TryGetValue(suffix, out ushort pointerOffset) && pointerOffset < 0x3FFF)
            {
                ushort pointer = (ushort)(0xC000 | pointerOffset);
                buffer.Add((byte)(pointer >> 8));
                buffer.Add((byte)(pointer & 0xFF));
                return;
            }

            if (buffer.Count < 0x3FFF)
            {
                compressionMap[suffix] = (ushort)buffer.Count;
            }

            byte[] labelBytes = Encoding.ASCII.GetBytes(labels[i]);
            if (labelBytes.Length > 63)
            {
                throw new ArgumentException($"DNS label '{labels[i]}' exceeds maximum allowed length of 63 bytes.");
            }

            buffer.Add((byte)labelBytes.Length);
            buffer.AddRange(labelBytes);
        }

        buffer.Add(0);
    }

    private static void EncodeResourceRecord(
        List<byte> buffer,
        DnsResourceRecord rr,
        Dictionary<string, ushort> compressionMap)
    {
        // 1. Encode Resource Record Owner Name with compression
        WriteDomainName(buffer, rr.Name, compressionMap);

        // 2. Header (Type, Class, TTL)
        byte[] rrHeader = new byte[8];
        BinaryPrimitives.WriteUInt16BigEndian(rrHeader.AsSpan(0, 2), (ushort)rr.Type);
        BinaryPrimitives.WriteUInt16BigEndian(rrHeader.AsSpan(2, 2), rr.Class == 0 ? (ushort)1 : rr.Class);
        BinaryPrimitives.WriteUInt32BigEndian(rrHeader.AsSpan(4, 4), (uint)rr.Ttl);
        buffer.AddRange(rrHeader);

        // 3. Reserve 2 bytes for RDATA Length
        int rdLengthIndex = buffer.Count;
        buffer.Add(0);
        buffer.Add(0);
        int rdataStartIndex = buffer.Count;

        // 4. Encode RDATA
        if (rr.ParsedIp != null)
        {
            byte[] ipBytes = rr.ParsedIp.GetAddressBytes();
            buffer.AddRange(ipBytes);
        }
        else if (IsDomainTargetRecord(rr.Type))
        {
            string? targetDomain = ExtractDomainString(rr);
            if (!string.IsNullOrEmpty(targetDomain))
            {
                WriteDomainName(buffer, targetDomain, compressionMap);
            }
            else if (rr.Data != null)
            {
                buffer.AddRange(rr.Data);
            }
        }
        else if (rr.Data != null)
        {
            buffer.AddRange(rr.Data);
        }

        // 5. Backfill actual RDATA length
        ushort rdataLength = (ushort)(buffer.Count - rdataStartIndex);
        buffer[rdLengthIndex] = (byte)(rdataLength >> 8);
        buffer[rdLengthIndex + 1] = (byte)(rdataLength & 0xFF);
    }

    private static bool IsDomainTargetRecord(DnsType type)
    {
        return type == DnsType.CNAME || type == DnsType.NS || type == DnsType.PTR || type == DnsType.DNAME;
    }

    private static string? ExtractDomainString(DnsResourceRecord rr)
    {
        if (rr.Data == null || rr.Data.Length == 0) return null;

        int offset = 0;
        if (DnsWireParser.TryReadDomainName(rr.Data, ref offset, out var domain) && !string.IsNullOrEmpty(domain))
        {
            return domain;
        }

        return null;
    }

    private static void EncodeEdnsOption(List<byte> buffer, ExtendedDnsError ede)
    {
        buffer.Add(0); // Root Domain Name "."

        byte[] optHeader = new byte[8];
        BinaryPrimitives.WriteUInt16BigEndian(optHeader.AsSpan(0, 2), 41); // Type OPT (41)
        BinaryPrimitives.WriteUInt16BigEndian(optHeader.AsSpan(2, 2), 4096); // UDP Payload Size
        BinaryPrimitives.WriteUInt32BigEndian(optHeader.AsSpan(4, 4), 0); // Extended RCODE / TTL
        buffer.AddRange(optHeader);

        byte[] extraTextBytes = string.IsNullOrEmpty(ede.ExtraText)
            ? Array.Empty<byte>()
            : Encoding.UTF8.GetBytes(ede.ExtraText);

        ushort optionDataLength = (ushort)(2 + extraTextBytes.Length);
        ushort totalRdataLength = (ushort)(4 + optionDataLength);

        byte[] rdLength = new byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(rdLength, totalRdataLength);
        buffer.AddRange(rdLength);

        byte[] optionHeader = new byte[4];
        BinaryPrimitives.WriteUInt16BigEndian(optionHeader.AsSpan(0, 2), 15); // Option Code 15 (EDE)
        BinaryPrimitives.WriteUInt16BigEndian(optionHeader.AsSpan(2, 2), optionDataLength);
        buffer.AddRange(optionHeader);

        byte[] infoCodeBytes = new byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(infoCodeBytes, (ushort)ede.InfoCode);
        buffer.AddRange(infoCodeBytes);

        if (extraTextBytes.Length > 0)
        {
            buffer.AddRange(extraTextBytes);
        }
    }
}
