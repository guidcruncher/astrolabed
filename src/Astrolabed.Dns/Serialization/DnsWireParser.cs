// File: src/Astrolabed.Dns/Serialization/DnsWireParser.cs
using System;
using System.Buffers.Binary;
using System.Text;
using Astrolabed.Dns.Models;

namespace Astrolabed.Dns.Serialization;

public static class DnsWireParser
{
    public static bool TryParse(ReadOnlySpan<byte> buffer, out DnsWireMessage? message)
    {
        message = null;
        if (buffer.Length < 12) return false;

        ushort id = BinaryPrimitives.ReadUInt16BigEndian(buffer[0..2]);
        ushort flags = BinaryPrimitives.ReadUInt16BigEndian(buffer[2..4]);

        ushort qdCount = BinaryPrimitives.ReadUInt16BigEndian(buffer[4..6]);
        ushort anCount = BinaryPrimitives.ReadUInt16BigEndian(buffer[6..8]);
        ushort nsCount = BinaryPrimitives.ReadUInt16BigEndian(buffer[8..10]);
        ushort arCount = BinaryPrimitives.ReadUInt16BigEndian(buffer[10..12]);

        var msg = new DnsWireMessage
        {
            TransactionId = id,
            IsResponse = (flags & 0x8000) != 0,
            OpCode = (DnsOpCode)((flags >> 11) & 0x0F),
            AuthoritativeAnswer = (flags & 0x0400) != 0,
            Truncated = (flags & 0x0200) != 0,
            RecursionDesired = (flags & 0x0100) != 0,
            RecursionAvailable = (flags & 0x0080) != 0,
            ResponseCode = (DnsResponseCode)(flags & 0x000F)
        };

        int offset = 12;

        if (qdCount > 0)
        {
            if (!TryReadDomainName(buffer, ref offset, out var qName)) return false;
            if (offset + 4 > buffer.Length) return false;

            msg.QuestionName = qName;
            msg.QuestionType = (DnsType)BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(offset, 2));
            msg.QuestionClass = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(offset + 2, 2));
            offset += 4;
        }

        for (int i = 0; i < anCount; i++)
        {
            if (!TryReadResourceRecord(buffer, ref offset, out var rr)) return false;
            msg.Answers.Add(rr!);
        }

        for (int i = 0; i < nsCount; i++)
        {
            if (!TryReadResourceRecord(buffer, ref offset, out var rr)) return false;
            msg.Authorities.Add(rr!);
        }

        for (int i = 0; i < arCount; i++)
        {
            if (!TryReadResourceRecord(buffer, ref offset, out var rr)) return false;
            
            if (rr!.Type == DnsType.OPT)
            {
                var edns = new EdnsOptions
                {
                    UdpPayloadSize = rr.Class,
                    ExtendedRCode = (byte)((rr.Ttl >> 24) & 0xFF),
                    Version = (byte)((rr.Ttl >> 16) & 0xFF),
                    DnssecOk = (rr.Ttl & 0x8000) != 0
                };

                ushort fullRCode = (ushort)(((ushort)edns.ExtendedRCode << 4) | ((ushort)msg.ResponseCode & 0x0F));
                msg.ResponseCode = (DnsResponseCode)fullRCode;

                ParseEdnsOptionData(rr.Data, edns);
                msg.Edns = edns;
            }
            else
            {
                msg.Additionals.Add(rr);
            }
        }

        message = msg;
        return true;
    }

    private static void ParseEdnsOptionData(ReadOnlySpan<byte> data, EdnsOptions edns)
    {
        int offset = 0;
        while (offset + 4 <= data.Length)
        {
            ushort code = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
            ushort len = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset + 2, 2));
            offset += 4;

            if (offset + len > data.Length) break;

            edns.Options.Add(new EdnsOptionCode
            {
                Code = code,
                Data = data.Slice(offset, len).ToArray()
            });

            offset += len;
        }
    }

    private static bool TryReadResourceRecord(ReadOnlySpan<byte> buffer, ref int offset, out DnsResourceRecord? record)
    {
        record = null;
        if (!TryReadDomainName(buffer, ref offset, out var name)) return false;
        if (offset + 10 > buffer.Length) return false;

        var type = (DnsType)BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(offset, 2));
        var cls = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(offset + 2, 2));
        var ttl = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(offset + 4, 4));
        var rdLength = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(offset + 8, 2));
        offset += 10;

        if (offset + rdLength > buffer.Length) return false;

        var data = buffer.Slice(offset, rdLength).ToArray();
        System.Net.IPAddress? parsedIp = null;

        if (type == DnsType.A && rdLength == 4)
        {
            parsedIp = new System.Net.IPAddress(data);
        }
        else if (type == DnsType.AAAA && rdLength == 16)
        {
            parsedIp = new System.Net.IPAddress(data);
        }

        offset += rdLength;

        record = new DnsResourceRecord
        {
            Name = name,
            Type = type,
            Class = cls,
            Ttl = ttl,
            Data = data,
            ParsedIp = parsedIp
        };
        return true;
    }

    private static bool TryReadDomainName(ReadOnlySpan<byte> buffer, ref int offset, out string domain)
    {
        domain = string.Empty;
        int currentOffset = offset;
        int maxJumps = 5;
        int jumpsPerformed = 0;
        int originalOffset = -1;

        var sb = new StringBuilder(64);

        while (currentOffset < buffer.Length)
        {
            byte length = buffer[currentOffset];
            if (length == 0)
            {
                currentOffset++;
                break;
            }

            if ((length & 0xC0) == 0xC0)
            {
                if (currentOffset + 1 >= buffer.Length) return false;
                if (originalOffset == -1) originalOffset = currentOffset + 2;

                ushort pointer = (ushort)(((length & 0x3F) << 8) | buffer[currentOffset + 1]);
                currentOffset = pointer;

                if (++jumpsPerformed > maxJumps) return false;
                continue;
            }

            currentOffset++;
            if (currentOffset + length > buffer.Length) return false;

            if (sb.Length > 0) sb.Append('.');
            sb.Append(Encoding.ASCII.GetString(buffer.Slice(currentOffset, length)));
            currentOffset += length;
        }

        offset = originalOffset != -1 ? originalOffset : currentOffset;
        domain = sb.ToString();
        return true;
    }
}
