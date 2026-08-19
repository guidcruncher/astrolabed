// File: src/Astrolabed.Dns/Serialization/DnsWireBuilder.cs
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;
using Astrolabed.Dns.Models;

namespace Astrolabed.Dns.Serialization;

public static class DnsWireBuilder
{
    public static byte[] BuildQuery(string domain, DnsType queryType, ushort transactionId, bool recursionDesired = true)
    {
        var buffer = new byte[512];
        int offset = 0;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(0, 2), transactionId);
        ushort flags = (ushort)(recursionDesired ? 0x0100 : 0x0000);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(2, 2), flags);

        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(4, 2), 1);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(6, 2), 0);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(8, 2), 0);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(10, 2), 1);
        offset = 12;

        EncodeDomainName(buffer.AsSpan(), ref offset, domain);

        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(offset, 2), (ushort)queryType);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(offset + 2, 2), 1);
        offset += 4;

        AppendOptRecord(buffer.AsSpan(), ref offset, 4096, 0, false, null);

        return buffer.AsSpan(0, offset).ToArray();
    }

    public static byte[] BuildResponse(
        DnsWireMessage request,
        DnsResponseCode rcode,
        IEnumerable<DnsResourceRecord>? answers = null,
        ExtendedDnsError? ede = null)
    {
        var buffer = new byte[4096];
        int offset = 0;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(0, 2), request.TransactionId);

        ushort baseRCode = (ushort)((ushort)rcode & 0x0F);
        ushort flags = 0x8400;
        if (request.RecursionDesired) flags |= 0x0100;
        flags |= baseRCode;
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(2, 2), flags);

        ushort anCount = 0;
        List<DnsResourceRecord>? answerList = answers != null ? new List<DnsResourceRecord>(answers) : null;
        if (answerList != null) anCount = (ushort)answerList.Count;

        bool includeOpt = request.Edns != null || ede != null;
        ushort arCount = (ushort)(includeOpt ? 1 : 0);

        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(4, 2), 1);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(6, 2), anCount);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(8, 2), 0);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(10, 2), arCount);
        offset = 12;

        EncodeDomainName(buffer.AsSpan(), ref offset, request.QuestionName);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(offset, 2), (ushort)request.QuestionType);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(offset + 2, 2), request.QuestionClass);
        offset += 4;

        if (answerList != null && rcode == DnsResponseCode.NoError)
        {
            foreach (var record in answerList)
            {
                EncodeDomainName(buffer.AsSpan(), ref offset, record.Name);
                BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(offset, 2), (ushort)record.Type);
                BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(offset + 2, 2), record.Class);
                BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(offset + 4, 4), record.Ttl);

                byte[] rdata = record.Data;
                if (record.ParsedIp != null)
                {
                    rdata = record.ParsedIp.GetAddressBytes();
                }

                BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(offset + 8, 2), (ushort)rdata.Length);
                offset += 10;

                rdata.CopyTo(buffer.AsSpan(offset));
                offset += rdata.Length;
            }
        }

        if (includeOpt)
        {
            ushort payloadSize = request.Edns?.UdpPayloadSize ?? 4096;
            byte extendedRCode = (byte)(((ushort)rcode >> 4) & 0xFF);
            bool dnssecOk = request.Edns?.DnssecOk ?? false;

            AppendOptRecord(buffer.AsSpan(), ref offset, payloadSize, extendedRCode, dnssecOk, ede);
        }

        return buffer.AsSpan(0, offset).ToArray();
    }

    private static void AppendOptRecord(
        Span<byte> buffer,
        ref int offset,
        ushort udpPayloadSize,
        byte extendedRCode,
        bool dnssecOk,
        ExtendedDnsError? ede)
    {
        buffer[offset++] = 0;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), (ushort)DnsType.OPT);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset + 2, 2), udpPayloadSize);
        offset += 4;

        uint ttlFlags = ((uint)extendedRCode << 24);
        if (dnssecOk) ttlFlags |= 0x8000;

        BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(offset, 4), ttlFlags);
        offset += 4;

        int rdLengthOffset = offset;
        offset += 2;

        int rdataStart = offset;

        if (ede != null)
        {
            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), 15);
            offset += 2;

            byte[] textBytes = string.IsNullOrEmpty(ede.ExtraText) ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(ede.ExtraText);
            ushort optionLen = (ushort)(2 + textBytes.Length);

            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), optionLen);
            offset += 2;

            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(offset, 2), (ushort)ede.InfoCode);
            offset += 2;

            if (textBytes.Length > 0)
            {
                textBytes.CopyTo(buffer.Slice(offset));
                offset += textBytes.Length;
            }
        }

        ushort totalRdLength = (ushort)(offset - rdataStart);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(rdLengthOffset, 2), totalRdLength);
    }

    public static void EncodeDomainName(Span<byte> buffer, ref int offset, string domain)
    {
        if (string.IsNullOrEmpty(domain))
        {
            buffer[offset++] = 0;
            return;
        }

        string[] labels = domain.Split('.');
        foreach (var label in labels)
        {
            byte len = (byte)Encoding.ASCII.GetBytes(label, buffer.Slice(offset + 1));
            buffer[offset] = len;
            offset += 1 + len;
        }

        buffer[offset++] = 0;
    }
}
