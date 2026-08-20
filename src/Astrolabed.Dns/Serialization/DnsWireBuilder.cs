// File: src/Astrolabed.Dns/Serialization/DnsWireBuilder.cs
using System;
using System.Buffers.Binary;
using System.Collections.Generic;

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

        // Authority & Additional Counts
        BinaryPrimitives.WriteUInt16BigEndian(header[8..10], 0);
        BinaryPrimitives.WriteUInt16BigEndian(header[10..12], ede != null ? (ushort)1 : (ushort)0);

        var buffer = new List<byte>();
        buffer.AddRange(header.ToArray());

        // Re-encode Question section to preserve original query domain format
        int offset = buffer.Count;
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
}
