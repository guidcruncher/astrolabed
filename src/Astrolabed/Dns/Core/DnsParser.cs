using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace Astrolabed.Dns.Core;

public static class DnsParser
{
    private const int MaxPointerJumps = 12;

    public static DnsMessage Parse(byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        if (buffer.Length < 12)
        {
            throw new InvalidOperationException("DNS message buffer too short for header parsing.");
        }

        var msg = new DnsMessage();
        int offset = 0;

        msg.Id = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset));
        offset += 2;

        ushort flags = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset));
        offset += 2;

        msg.IsResponse = (flags & 0x8000) != 0;
        msg.ResponseCode = (DnsResponseCode)(flags & 0x000F);

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

            if (offset + 4 > buffer.Length)
            {
                throw new InvalidOperationException("Truncated question section entry.");
            }

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

            if (offset + 10 > buffer.Length)
            {
                throw new InvalidOperationException("Truncated resource record header.");
            }

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
                throw new InvalidOperationException("RDATA length exceeds buffer bounds.");
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
        var sb = new StringBuilder(64);
        int currentOffset = offset;
        int originalOffset = offset;
        bool jumped = false;
        int jumpsCount = 0;

        while (true)
        {
            if (currentOffset >= buffer.Length)
            {
                throw new InvalidOperationException("Name offset exceeds buffer bounds.");
            }

            byte len = buffer[currentOffset++];

            if (len == 0)
            {
                if (!jumped)
                {
                    offset = currentOffset;
                }
                break;
            }

            // Compression pointer check (top 2 bits set: 0xC0)
            if ((len & 0xC0) == 0xC0)
            {
                if (currentOffset >= buffer.Length)
                {
                    throw new InvalidOperationException("Compression pointer offset exceeds buffer bounds.");
                }

                if (++jumpsCount > MaxPointerJumps)
                {
                    throw new InvalidOperationException("Circular or excessive pointer jumps detected in DNS header.");
                }

                byte b2 = buffer[currentOffset];
                int pointer = ((len & 0x3F) << 8) | b2;

                if (pointer >= originalOffset && jumped)
                {
                    throw new InvalidOperationException("Invalid forward or cyclic compression pointer detected.");
                }

                if (!jumped)
                {
                    offset = currentOffset + 1;
                    jumped = true;
                }

                currentOffset = pointer;
                continue;
            }

            // Handle invalid label format flags
            if ((len & 0xC0) != 0)
            {
                throw new InvalidOperationException($"Unsupported DNS label prefix flag 0x{len:X2}.");
            }

            if (currentOffset + len > buffer.Length)
            {
                throw new InvalidOperationException("Label length exceeds buffer bounds.");
            }

            if (sb.Length > 0)
            {
                sb.Append('.');
            }

            sb.Append(Encoding.ASCII.GetString(buffer, currentOffset, len));
            currentOffset += len;

            if (!jumped)
            {
                offset = currentOffset;
            }
        }

        return (sb.ToString(), offset);
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

        ushort flags = 0x8003; // QR = 1 (Response), RCODE = 3 (NXDOMAIN)
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

        ReadOnlySpan<char> span = name.AsSpan().Trim('.');
        if (span.IsEmpty)
        {
            return 1;
        }

        int length = 0;
        int separatorIndex;

        while ((separatorIndex = span.IndexOf('.')) != -1)
        {
            var label = span[..separatorIndex];
            if (!label.IsEmpty)
            {
                length += 1 + Encoding.ASCII.GetByteCount(label);
            }
            span = span[(separatorIndex + 1)..];
        }

        if (!span.IsEmpty)
        {
            length += 1 + Encoding.ASCII.GetByteCount(span);
        }

        return length + 1; // Null terminator byte
    }

    private static int WriteNameWire(Span<byte> destination, string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            destination[0] = 0;
            return 1;
        }

        ReadOnlySpan<char> span = name.AsSpan().Trim('.');
        if (span.IsEmpty)
        {
            destination[0] = 0;
            return 1;
        }

        int written = 0;
        int separatorIndex;

        while ((separatorIndex = span.IndexOf('.')) != -1)
        {
            var label = span[..separatorIndex];
            if (!label.IsEmpty)
            {
                int byteCount = Encoding.ASCII.GetByteCount(label);
                destination[written++] = (byte)byteCount;
                Encoding.ASCII.GetBytes(label, destination.Slice(written, byteCount));
                written += byteCount;
            }
            span = span[(separatorIndex + 1)..];
        }

        if (!span.IsEmpty)
        {
            int byteCount = Encoding.ASCII.GetByteCount(span);
            destination[written++] = (byte)byteCount;
            Encoding.ASCII.GetBytes(span, destination.Slice(written, byteCount));
            written += byteCount;
        }

        destination[written++] = 0;
        return written;
    }
}
