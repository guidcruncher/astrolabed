using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Text;

using Astrolabed.Dns.Models;

namespace Astrolabed.Dns.Serialization;

/// <summary>
/// Provides zero-allocation, high-performance binary parsing for RFC 1035 wire-format DNS messages.
/// </summary>
public static class DnsWireParser
{
    private const int HeaderSize = 12;
    private const int MaxCompressionJumps = 8;
    private const byte CompressionMask = 0xC0;

    /// <summary>
    /// Attempts to parse a raw binary DNS wire message.
    /// </summary>
    /// <param name="buffer">The binary span containing the raw DNS packet.</param>
    /// <param name="message">The parsed <see cref="DnsWireMessage"/> instance if successful; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryParse(ReadOnlySpan<byte> buffer, out DnsWireMessage? message)
    {
        message = null;
        if (buffer.Length < HeaderSize)
        {
            return false;
        }

        ushort id = BinaryPrimitives.ReadUInt16BigEndian(buffer[..2]);
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

        int offset = HeaderSize;

        // Parse Question Section
        if (qdCount > 0)
        {
            if (!TryReadDomainName(buffer, ref offset, out string qName))
            {
                return false;
            }

            if (offset + 4 > buffer.Length)
            {
                return false;
            }

            msg.QuestionName = qName;
            msg.QuestionType = (DnsType)BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(offset, 2));
            msg.QuestionClass = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(offset + 2, 2));
            offset += 4;
        }

        // Parse Answer Section
        for (int i = 0; i < anCount; i++)
        {
            if (!TryReadResourceRecord(buffer, ref offset, out DnsResourceRecord? rr) || rr is null)
            {
                return false;
            }

            msg.Answers.Add(rr);
        }

        // Parse Authority Section
        for (int i = 0; i < nsCount; i++)
        {
            if (!TryReadResourceRecord(buffer, ref offset, out DnsResourceRecord? rr) || rr is null)
            {
                return false;
            }

            msg.Authorities.Add(rr);
        }

        // Parse Additional Section (including EDNS0 OPT RR)
        for (int i = 0; i < arCount; i++)
        {
            if (!TryReadResourceRecord(buffer, ref offset, out DnsResourceRecord? rr) || rr is null)
            {
                return false;
            }

            if (rr.Type == DnsType.OPT)
            {
                var edns = new EdnsOptions
                {
                    UdpPayloadSize = rr.Class,
                    ExtendedRCode = (byte)((rr.Ttl >> 24) & 0xFF),
                    Version = (byte)((rr.Ttl >> 16) & 0xFF),
                    DnssecOk = (rr.Ttl & 0x8000) != 0
                };

                // Combine upper 8 bits from OPT TTL with lower 4 bits from Header RCODE
                ushort fullRCode = (ushort)(((ushort)edns.ExtendedRCode << 4) | ((ushort)msg.ResponseCode & 0x0F));
                msg.ResponseCode = (DnsResponseCode)fullRCode;

                if (rr.Data is not null)
                {
                    ParseEdnsOptionData(rr.Data, edns);
                }

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

    /// <summary>
    /// Reads and parses an RFC 1035 domain name from a DNS buffer with pointer compression support.
    /// </summary>
    /// <param name="buffer">The full DNS message buffer.</param>
    /// <param name="offset">Current reading offset, advanced past the domain name upon return.</param>
    /// <param name="domain">The parsed, fully qualified domain name string.</param>
    /// <returns><c>true</c> if the domain name was read successfully; otherwise, <c>false</c>.</returns>
    public static bool TryReadDomainName(ReadOnlySpan<byte> buffer, ref int offset, out string domain)
    {
        domain = string.Empty;
        int currentOffset = offset;
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

            byte labelType = (byte)(length & CompressionMask);

            // Pointer compression handling (11xxxxxx)
            if (labelType == CompressionMask)
            {
                if (currentOffset + 1 >= buffer.Length)
                {
                    return false;
                }

                if (originalOffset == -1)
                {
                    originalOffset = currentOffset + 2;
                }

                ushort pointer = (ushort)(((length & 0x3F) << 8) | buffer[currentOffset + 1]);
                if (pointer >= buffer.Length)
                {
                    return false;
                }

                currentOffset = pointer;

                if (++jumpsPerformed > MaxCompressionJumps)
                {
                    return false; // Circular compression pointer detected
                }

                continue;
            }

            // Unrecognized label type
            if (labelType != 0x00)
            {
                return false;
            }

            currentOffset++;
            if (currentOffset + length > buffer.Length)
            {
                return false;
            }

            if (sb.Length > 0)
            {
                sb.Append('.');
            }

            sb.Append(Encoding.ASCII.GetString(buffer.Slice(currentOffset, length)));
            currentOffset += length;
        }

        offset = originalOffset != -1 ? originalOffset : currentOffset;
        domain = sb.ToString();
        return true;
    }

    private static bool TryReadResourceRecord(ReadOnlySpan<byte> buffer, ref int offset, out DnsResourceRecord? record)
    {
        record = null;
        if (!TryReadDomainName(buffer, ref offset, out string name))
        {
            return false;
        }

        if (offset + 10 > buffer.Length)
        {
            return false;
        }

        var type = (DnsType)BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(offset, 2));
        ushort cls = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(offset + 2, 2));
        uint ttl = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(offset + 4, 4));
        ushort rdLength = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(offset + 8, 2));
        offset += 10;

        if (offset + rdLength > buffer.Length)
        {
            return false;
        }

        ReadOnlySpan<byte> rdataSpan = buffer.Slice(offset, rdLength);
        byte[] data;
        IPAddress? parsedIp = null;

        if (type == DnsType.A && rdLength == 4)
        {
            data = rdataSpan.ToArray();
            parsedIp = new IPAddress(rdataSpan);
        }
        else if (type == DnsType.AAAA && rdLength == 16)
        {
            data = rdataSpan.ToArray();
            parsedIp = new IPAddress(rdataSpan);
        }
        else if (type is DnsType.CNAME or DnsType.NS or DnsType.PTR or DnsType.DNAME)
        {
            int rdataOffset = offset;
            if (!TryReadDomainName(buffer, ref rdataOffset, out string targetDomain))
            {
                return false;
            }

            data = EncodeUncompressedDomainName(targetDomain);
        }
        else
        {
            data = rdataSpan.ToArray();
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

    private static void ParseEdnsOptionData(ReadOnlySpan<byte> data, EdnsOptions edns)
    {
        int offset = 0;
        while (offset + 4 <= data.Length)
        {
            ushort code = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
            ushort len = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset + 2, 2));
            offset += 4;

            if (offset + len > data.Length)
            {
                break;
            }

            edns.Options.Add(new EdnsOptionCode
            {
                Code = code,
                Data = data.Slice(offset, len).ToArray()
            });

            offset += len;
        }
    }

    private static byte[] EncodeUncompressedDomainName(string domain)
    {
        if (string.IsNullOrEmpty(domain) || domain == ".")
        {
            return [0];
        }

        ReadOnlySpan<char> span = domain.AsSpan().TrimEnd('.');
        var writer = new ArrayBufferWriter<byte>(span.Length + 2);

        int currentOffset = 0;
        while (currentOffset < span.Length)
        {
            int nextDot = span[currentOffset..].IndexOf('.');
            ReadOnlySpan<char> label = nextDot < 0
                ? span[currentOffset..]
                : span.Slice(currentOffset, nextDot);

            int byteCount = Encoding.ASCII.GetByteCount(label);
            Span<byte> labelBuffer = writer.GetSpan(1 + byteCount);
            labelBuffer[0] = (byte)byteCount;
            Encoding.ASCII.GetBytes(label, labelBuffer[1..]);
            writer.Advance(1 + byteCount);

            if (nextDot < 0)
            {
                break;
            }

            currentOffset += nextDot + 1;
        }

        Span<byte> nullTerminator = writer.GetSpan(1);
        nullTerminator[0] = 0;
        writer.Advance(1);

        return writer.WrittenSpan.ToArray();
    }
}
