using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace Astrolabed.Dns.Core;

public static class DnsParser
{
    private const int MaxPointerJumps = 128;

    public static DnsMessage Parse(byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        if (buffer.Length < 12)
        {
            throw new InvalidOperationException("DNS message too short");
        }

        var msg = new DnsMessage();
        int offset = 0;

        msg.Id = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset));
        offset += 2;

        ushort flags = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset));
        offset += 2;

        msg.IsResponse = (flags & 0x8000) != 0;
        msg.ResponseCode = ((flags & 0x000F)).ToString();

        ushort qdCount = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset));
        offset += 2;
        ushort anCount = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset));
        offset += 2;
        ushort nsCount = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset));
        offset += 2;
        ushort arCount = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset));
        offset += 2;

        // Question Section
        for (int i = 0; i < qdCount; i++)
        {
            var (name, newOffset) = ReadName(buffer, offset);
            offset = newOffset;

            ushort rawType = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset));
            offset += 2;
            ushort cls = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset));
            offset += 2;

            msg.Questions.Add(new DnsQuestion
            {
                Name = name,
                Type = (DnsType)rawType,
                Class = cls
            });
        }

        // Answer Section
        offset = ParseResourceRecords(buffer, offset, anCount, msg.Answers);

        // Authority Section
        offset = ParseResourceRecords(buffer, offset, nsCount, msg.Authorities);

        // Additional Section
        _ = ParseResourceRecords(buffer, offset, arCount, msg.Additionals);

        return msg;
    }

    private static int ParseResourceRecords(byte[] buffer, int offset, int count, List<DnsResourceRecord> targetList)
    {
        for (int i = 0; i < count; i++)
        {
            var (name, newOffset) = ReadName(buffer, offset);
            offset = newOffset;

            ushort rawType = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset));
            offset += 2;
            ushort cls = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset));
            offset += 2;
            int ttl = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(offset));
            offset += 4;
            ushort rdLength = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset));
            offset += 2;

            if (offset + rdLength > buffer.Length)
            {
                throw new InvalidOperationException("RDATA length exceeds buffer bounds");
            }

            byte[] rdata = buffer.AsSpan(offset, rdLength).ToArray();
            offset += rdLength;

            targetList.Add(new DnsResourceRecord
            {
                Name = name,
                Type = (DnsType)rawType,
                Class = cls,
                Ttl = ttl,
                RData = rdata
            });
        }

        return offset;
    }

    private static (string name, int offset) ReadName(byte[] buffer, int offset)
    {
        var labels = new List<string>();
        int originalOffset = offset;
        bool jumped = false;
        int jumpOffset = -1;
        int jumpsCount = 0;

        while (true)
        {
            if (offset >= buffer.Length)
            {
                throw new InvalidOperationException("Name exceeds buffer bounds");
            }

            byte len = buffer[offset++];

            if (len == 0)
            {
                break;
            }

            // Compression pointer check (top 2 bits set: 0xC0)
            if ((len & 0xC0) == 0xC0)
            {
                if (offset >= buffer.Length)
                {
                    throw new InvalidOperationException("Pointer exceeds buffer bounds");
                }

                if (++jumpsCount > MaxPointerJumps)
                {
                    throw new InvalidOperationException("Circular pointer loop detected in DNS compression header");
                }

                byte b2 = buffer[offset++];
                int pointer = ((len & 0x3F) << 8) | b2;

                if (!jumped)
                {
                    jumpOffset = offset;
                    jumped = true;
                }

                offset = pointer;
                continue;
            }

            if (offset + len > buffer.Length)
            {
                throw new InvalidOperationException("Label length exceeds buffer bounds");
            }

            string label = Encoding.ASCII.GetString(buffer, offset, len);
            offset += len;
            labels.Add(label);
        }

        if (jumped && jumpOffset != -1)
        {
            offset = jumpOffset;
        }

        string name = string.Join(".", labels);
        return (name, offset);
    }

    public static byte[] BuildBlockedResponse(DnsMessage request)
    {
        ArgumentNullException.ThrowIfNull(request);

        int qSize = 0;
        foreach (var q in request.Questions)
        {
            qSize += GetNameWireLength(q.Name) + 4;
        }

        var buffer = new byte[12 + qSize];
        int offset = 0;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(offset), request.Id);
        offset += 2;

        ushort flags = 0x8003; // QR = 1 (response), RCODE = 3 (NXDOMAIN)
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(offset), flags);
        offset += 2;

        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(offset), (ushort)request.Questions.Count);
        offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(offset), 0); // ANCOUNT
        offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(offset), 0); // NSCOUNT
        offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(offset), 0); // ARCOUNT
        offset += 2;

        foreach (var q in request.Questions)
        {
            int written = WriteNameWire(buffer.AsSpan(offset), q.Name);
            offset += written;

            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(offset), (ushort)q.Type);
            offset += 2;
            BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(offset), (ushort)q.Class);
            offset += 2;
        }

        return buffer;
    }

    private static int GetNameWireLength(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return 1;
        }

        var parts = name.Split('.', StringSplitOptions.RemoveEmptyEntries);
        int len = 0;
        foreach (var p in parts)
        {
            len += 1 + Encoding.ASCII.GetByteCount(p);
        }
        len += 1; // Null terminator
        return len;
    }

    private static int WriteNameWire(Span<byte> destination, string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            destination[0] = 0;
            return 1;
        }

        int written = 0;
        var parts = name.Split('.', StringSplitOptions.RemoveEmptyEntries);

        foreach (var p in parts)
        {
            int byteCount = Encoding.ASCII.GetByteCount(p);
            destination[written++] = (byte)byteCount;
            Encoding.ASCII.GetBytes(p, destination.Slice(written, byteCount));
            written += byteCount;
        }

        destination[written++] = 0;
        return written;
    }
}
