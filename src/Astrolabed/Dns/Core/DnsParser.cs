using System;
using System.Buffers.Binary;
using System.Text;

namespace Astrolabed.Dns.Core;

public static class DnsParser
{
    private const int MaxDomainNameLength = 255;
    private const int MaxCompressionJumps = 64;

    public static DnsMessage Parse(byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        if (buffer.Length < 12)
        {
            throw new InvalidOperationException("DNS message too short");
        }

        ReadOnlySpan<byte> span = buffer;
        var msg = new DnsMessage();
        int offset = 0;

        msg.Id = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset));
        offset += 2;

        ushort flags = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset));
        offset += 2;

        msg.IsResponse = (flags & 0x8000) != 0;
        msg.ResponseCode = (flags & 0x000F).ToString();

        ushort qdCount = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset));
        offset += 2;
        ushort anCount = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset));
        offset += 2;
        ushort nsCount = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset));
        offset += 2;
        ushort arCount = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset));
        offset += 2;

        for (int i = 0; i < qdCount; i++)
        {
            var (name, newOffset) = ReadName(buffer, offset);
            offset = newOffset;

            ushort type = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset));
            offset += 2;
            ushort cls = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset));
            offset += 2;

            msg.Questions.Add(new DnsQuestion
            {
                Name = name,
                Type = type,
                Class = cls
            });
        }

        for (int i = 0; i < anCount; i++)
        {
            var (name, newOffset) = ReadName(buffer, offset);
            offset = newOffset;

            ushort type = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset));
            offset += 2;
            ushort cls = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset));
            offset += 2;
            int ttl = BinaryPrimitives.ReadInt32BigEndian(span.Slice(offset));
            offset += 4;
            ushort rdLength = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(offset));
            offset += 2;

            if (offset + rdLength > buffer.Length)
            {
                throw new InvalidOperationException("RDATA length exceeds buffer");
            }

            var rdata = GC.AllocateUninitializedArray<byte>(rdLength);
            span.Slice(offset, rdLength).CopyTo(rdata);
            offset += rdLength;

            msg.Answers.Add(new DnsResourceRecord
            {
                Name = name,
                Type = type,
                Class = cls,
                Ttl = ttl,
                RData = rdata
            });
        }

        return msg;
    }

    private static (string name, int offset) ReadName(byte[] buffer, int offset)
    {
        Span<char> charBuffer = stackalloc char[MaxDomainNameLength];
        int charCount = 0;
        int jumpOffset = -1;
        bool jumped = false;
        int jumps = 0;

        while (true)
        {
            if (offset >= buffer.Length)
            {
                throw new InvalidOperationException("Name exceeds buffer");
            }

            byte len = buffer[offset++];

            if (len == 0)
            {
                break;
            }

            if ((len & 0xC0) == 0xC0)
            {
                if (offset >= buffer.Length)
                {
                    throw new InvalidOperationException("Pointer exceeds buffer");
                }

                if (++jumps > MaxCompressionJumps)
                {
                    throw new InvalidOperationException("Cyclic DNS compression pointer detected");
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
                throw new InvalidOperationException("Label exceeds buffer");
            }

            if (charCount + len + (charCount > 0 ? 1 : 0) > MaxDomainNameLength)
            {
                throw new InvalidOperationException("Domain name exceeds maximum length");
            }

            if (charCount > 0)
            {
                charBuffer[charCount++] = '.';
            }

            Encoding.ASCII.GetChars(buffer.AsSpan(offset, len), charBuffer.Slice(charCount, len));
            charCount += len;
            offset += len;
        }

        if (jumped && jumpOffset != -1)
        {
            offset = jumpOffset;
        }

        var name = new string(charBuffer[..charCount]);
        return (name, offset);
    }

    public static byte[] BuildBlockedResponse(DnsMessage request)
    {
        ArgumentNullException.ThrowIfNull(request);

        int qSize = 0;
        int questionCount = request.Questions.Count;

        for (int i = 0; i < questionCount; i++)
        {
            qSize += GetNameWireLength(request.Questions[i].Name) + 4;
        }

        var buffer = new byte[12 + qSize];
        Span<byte> span = buffer;
        int offset = 0;

        BinaryPrimitives.WriteUInt16BigEndian(span.Slice(offset), request.Id);
        offset += 2;

        ushort flags = 0x8003; // QR = 1 (Response), RCODE = 3 (NXDOMAIN)
        BinaryPrimitives.WriteUInt16BigEndian(span.Slice(offset), flags);
        offset += 2;

        BinaryPrimitives.WriteUInt16BigEndian(span.Slice(offset), (ushort)questionCount);
        offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(span.Slice(offset), 0); // ANCOUNT
        offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(span.Slice(offset), 0); // NSCOUNT
        offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(span.Slice(offset), 0); // ARCOUNT
        offset += 2;

        for (int i = 0; i < questionCount; i++)
        {
            var q = request.Questions[i];
            WriteNameWire(span, ref offset, q.Name);
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(offset), (ushort)q.Type);
            offset += 2;
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(offset), (ushort)q.Class);
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

        int length = 0;
        int labelLen = 0;

        for (int i = 0; i < name.Length; i++)
        {
            if (name[i] == '.')
            {
                if (labelLen > 0)
                {
                    length += 1 + labelLen;
                    labelLen = 0;
                }
            }
            else
            {
                labelLen++;
            }
        }

        if (labelLen > 0)
        {
            length += 1 + labelLen;
        }

        return length + 1; // null terminator
    }

    private static void WriteNameWire(Span<byte> buffer, ref int offset, string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            buffer[offset++] = 0;
            return;
        }

        int labelStart = 0;

        for (int i = 0; i <= name.Length; i++)
        {
            if (i == name.Length || name[i] == '.')
            {
                int labelLen = i - labelStart;
                if (labelLen > 0)
                {
                    buffer[offset++] = (byte)labelLen;
                    Encoding.ASCII.GetBytes(name.AsSpan(labelStart, labelLen), buffer.Slice(offset));
                    offset += labelLen;
                }
                labelStart = i + 1;
            }
        }

        buffer[offset++] = 0;
    }
}
