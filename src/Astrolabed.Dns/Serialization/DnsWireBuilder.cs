// File: src/Astrolabed.Dns/Serialization/DnsWireBuilder.cs
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
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

        // 1. Crucial: Preserve incoming Transaction ID
        BinaryPrimitives.WriteUInt16BigEndian(header[0..2], request.TransactionId);

        // 2. Response Flags: QR=1 (Response), Opcode=0, AA=1, RD=1, RA=1, RCODE
        ushort flags = 0x8180; // Standard response flags
        flags |= (ushort)((byte)responseCode & 0x0F);
        BinaryPrimitives.WriteUInt16BigEndian(header[2..4], flags);

        // 3. Question Count (1)
        BinaryPrimitives.WriteUInt16BigEndian(header[4..6], 1);

        // Calculate Answer Count
        var answerList = answers != null ? new List<DnsResourceRecord>(answers) : new List<DnsResourceRecord>();
        BinaryPrimitives.WriteUInt16BigEndian(header[6..8], (ushort)answerList.Count);

        // Authority Count (0)
        BinaryPrimitives.WriteUInt16BigEndian(header[8..10], 0);

        // Additional Count (1 if EDE present for EDNS0 OPT RR, else 0)
        BinaryPrimitives.WriteUInt16BigEndian(header[10..12], ede != null ? (ushort)1 : (ushort)0);

        var buffer = new List<byte>();
        buffer.AddRange(header.ToArray());

        // Re-encode Question section to preserve original query domain format
        byte[] domainBuffer = new byte[256];
        int domainOffset = 0;
        EncodeDomainName(domainBuffer, ref domainOffset, request.QuestionName);

        for (int i = 0; i < domainOffset; i++)
        {
            buffer.Add(domainBuffer[i]);
        }

        byte[] typeAndClass = new byte[4];
        BinaryPrimitives.WriteUInt16BigEndian(typeAndClass.AsSpan(0, 2), (ushort)request.QuestionType);
        BinaryPrimitives.WriteUInt16BigEndian(typeAndClass.AsSpan(2, 2), 1); // Class IN
        buffer.AddRange(typeAndClass);

        // Encode Answer RRs
        foreach (var rr in answerList)
        {
            EncodeResourceRecord(buffer, rr);
        }

        // Encode EDNS0 OPT Pseudo-RR containing Extended DNS Error (RFC 8914) if provided
        if (ede != null)
        {
            EncodeEdnsOption(buffer, ede);
        }

        return buffer.ToArray();
    }

    public static void EncodeDomainName(byte[] buffer, ref int offset, string domain)
    {
        string[] labels = domain.TrimEnd('.').Split('.');
        foreach (var label in labels)
        {
            buffer[offset++] = (byte)label.Length;
            for (int i = 0; i < label.Length; i++)
            {
                buffer[offset++] = (byte)label[i];
            }
        }
        buffer[offset++] = 0; // Root label terminator
    }

    private static void EncodeResourceRecord(List<byte> buffer, DnsResourceRecord rr)
    {
        byte[] domainBuffer = new byte[256];
        int domainOffset = 0;
        EncodeDomainName(domainBuffer, ref domainOffset, rr.Name);

        for (int i = 0; i < domainOffset; i++)
        {
            buffer.Add(domainBuffer[i]);
        }

        byte[] rrHeader = new byte[8];
        BinaryPrimitives.WriteUInt16BigEndian(rrHeader.AsSpan(0, 2), (ushort)rr.Type);
        BinaryPrimitives.WriteUInt16BigEndian(rrHeader.AsSpan(2, 2), rr.Class == 0 ? (ushort)1 : rr.Class);
        BinaryPrimitives.WriteUInt32BigEndian(rrHeader.AsSpan(4, 4), (uint)rr.Ttl);
        buffer.AddRange(rrHeader);

        if (rr.ParsedIp != null)
        {
            byte[] ipBytes = rr.ParsedIp.GetAddressBytes();
            byte[] rdLength = new byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(rdLength, (ushort)ipBytes.Length);
            buffer.AddRange(rdLength);
            buffer.AddRange(ipBytes);
        }
        else if (rr.Data != null)
        {
            byte[] rdLength = new byte[2];
            BinaryPrimitives.WriteUInt16BigEndian(rdLength, (ushort)rr.Data.Length);
            buffer.AddRange(rdLength);
            buffer.AddRange(rr.Data);
        }
    }

    private static void EncodeEdnsOption(List<byte> buffer, ExtendedDnsError ede)
    {
        buffer.Add(0); // Root Domain Name "."

        byte[] optHeader = new byte[8];
        BinaryPrimitives.WriteUInt16BigEndian(optHeader.AsSpan(0, 2), 41); // Type OPT (41)
        BinaryPrimitives.WriteUInt16BigEndian(optHeader.AsSpan(2, 2), 4096); // UDP Payload Size (4096)
        // TTL field set to Extended RCODE=0, Version=0, Flags=0
        BinaryPrimitives.WriteUInt32BigEndian(optHeader.AsSpan(4, 4), 0);
        buffer.AddRange(optHeader);

        byte[] extraTextBytes = string.IsNullOrEmpty(ede.ExtraText)
            ? Array.Empty<byte>()
            : Encoding.UTF8.GetBytes(ede.ExtraText);

        ushort optionDataLength = (ushort)(2 + extraTextBytes.Length);
        ushort totalRdataLength = (ushort)(4 + optionDataLength); // OptionCode (2) + OptionLength (2) + OptionDataLength

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
