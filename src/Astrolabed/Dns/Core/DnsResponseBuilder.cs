using System;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace Astrolabed.Dns.Core;

public static class DnsResponseBuilder
{
    private const int HeaderSize = 12;
    private const ushort OptionCodeEdnsError = 15;

    public static int GetQuestionEnd(ReadOnlySpan<byte> req)
    {
        if (req.Length < HeaderSize)
        {
            throw new ArgumentException("Request too short", nameof(req));
        }

        ushort qdcount = BinaryPrimitives.ReadUInt16BigEndian(req.Slice(4, 2));
        int offset = HeaderSize;

        for (int q = 0; q < qdcount; q++)
        {
            while (true)
            {
                if (offset >= req.Length)
                {
                    throw new ArgumentException("Malformed DNS packet: label overruns packet", nameof(req));
                }

                byte len = req[offset++];
                if (len == 0)
                {
                    break;
                }

                if ((len & 0xC0) == 0xC0)
                {
                    if (offset >= req.Length)
                    {
                        throw new ArgumentException("Malformed DNS packet: incomplete pointer", nameof(req));
                    }
                    offset++;
                    break;
                }

                offset += len;
                if (offset > req.Length)
                {
                    throw new ArgumentException("Malformed DNS packet: label overruns packet", nameof(req));
                }
            }

            if (offset + 4 > req.Length)
            {
                throw new ArgumentException("Malformed DNS packet: missing QTYPE/QCLASS", nameof(req));
            }

            offset += 4;
        }

        return offset;
    }

    private static int SkipName(ReadOnlySpan<byte> buf, int offset)
    {
        int len = buf.Length;
        while (true)
        {
            if (offset >= len)
            {
                throw new ArgumentException("Malformed DNS packet while parsing name", nameof(buf));
            }

            byte b = buf[offset++];
            if (b == 0)
            {
                break;
            }

            if ((b & 0xC0) == 0xC0)
            {
                if (offset >= len)
                {
                    throw new ArgumentException("Malformed DNS packet: incomplete pointer", nameof(buf));
                }
                offset++;
                break;
            }

            offset += b;
            if (offset > len)
            {
                throw new ArgumentException("Malformed DNS packet: label overruns packet", nameof(buf));
            }
        }

        return offset;
    }

    private static bool TryGetAdditionalSection(ReadOnlySpan<byte> req, out int addStart, out int addLength, out ushort arCount)
    {
        addStart = 0;
        addLength = 0;
        arCount = 0;

        try
        {
            if (req.Length < HeaderSize)
            {
                return false;
            }

            arCount = BinaryPrimitives.ReadUInt16BigEndian(req.Slice(10, 2));
            if (arCount == 0)
            {
                return true;
            }

            int offset = GetQuestionEnd(req);
            addStart = offset;

            for (int i = 0; i < arCount; i++)
            {
                offset = SkipName(req, offset);

                if (offset + 10 > req.Length)
                {
                    return false;
                }

                ushort rdlen = BinaryPrimitives.ReadUInt16BigEndian(req.Slice(offset + 8, 2));
                offset += 10 + rdlen;

                if (offset > req.Length)
                {
                    return false;
                }
            }

            addLength = offset - addStart;
            return true;
        }
        catch
        {
            addStart = 0;
            addLength = 0;
            arCount = 0;
            return false;
        }
    }

    public static byte[] CopyQuestionBytes(byte[] req)
    {
        ArgumentNullException.ThrowIfNull(req);
        int end = GetQuestionEnd(req);
        int len = end - HeaderSize;
        var outBytes = GC.AllocateUninitializedArray<byte>(len);
        req.AsSpan(HeaderSize, len).CopyTo(outBytes);
        return outBytes;
    }

    public static byte[] BuildRcodeResponse(byte[] req, int rcode)
    {
        ArgumentNullException.ThrowIfNull(req);

        ushort id = BinaryPrimitives.ReadUInt16BigEndian(req.AsSpan(0, 2));
        byte reqFlagsHi = req[2];
        byte flagsHi = (byte)((reqFlagsHi & 0x01) | 0x80);
        byte flagsLo = (byte)(0x80 | (rcode & 0x0F));
        ushort qdCount = BinaryPrimitives.ReadUInt16BigEndian(req.AsSpan(4, 2));

        int qEnd = GetQuestionEnd(req);
        int qLen = qEnd - HeaderSize;

        TryGetAdditionalSection(req, out int addStart, out int addLength, out ushort arCount);

        int totalLen = HeaderSize + qLen + addLength;
        var resp = GC.AllocateUninitializedArray<byte>(totalLen);
        Span<byte> span = resp;

        BinaryPrimitives.WriteUInt16BigEndian(span.Slice(0, 2), id);
        span[2] = flagsHi;
        span[3] = flagsLo;
        BinaryPrimitives.WriteUInt16BigEndian(span.Slice(4, 2), qdCount);
        BinaryPrimitives.WriteUInt16BigEndian(span.Slice(6, 2), 0);
        BinaryPrimitives.WriteUInt16BigEndian(span.Slice(8, 2), 0);
        BinaryPrimitives.WriteUInt16BigEndian(span.Slice(10, 2), arCount);

        req.AsSpan(HeaderSize, qLen).CopyTo(span.Slice(HeaderSize));

        if (addLength > 0)
        {
            req.AsSpan(addStart, addLength).CopyTo(span.Slice(HeaderSize + qLen));
        }

        return resp;
    }

    public static byte[] BuildRcodeResponseWithEde(byte[] req, int rcode, ushort edeCode)
    {
        byte[] baseResponse = BuildRcodeResponse(req, rcode);
        if (baseResponse.Length < HeaderSize)
        {
            return baseResponse;
        }

        return AttachEdeOption(baseResponse, edeCode);
    }

    public static byte[] BuildStaticIpResponse(byte[] req, IPAddress ip, int ttlSeconds = 60)
    {
        ArgumentNullException.ThrowIfNull(req);
        ArgumentNullException.ThrowIfNull(ip);

        ushort id = BinaryPrimitives.ReadUInt16BigEndian(req.AsSpan(0, 2));
        byte reqFlagsHi = req[2];
        byte flagsHi = (byte)((reqFlagsHi & 0x01) | 0x80);
        byte flagsLo = 0x80;
        ushort qdCount = BinaryPrimitives.ReadUInt16BigEndian(req.AsSpan(4, 2));

        int qEnd = GetQuestionEnd(req);
        int qLen = qEnd - HeaderSize;

        TryGetAdditionalSection(req, out int addStart, out int addLength, out ushort arCount);

        bool isIpv4 = ip.AddressFamily == AddressFamily.InterNetwork;
        ushort qType = (ushort)(isIpv4 ? 1 : 28);
        int ipLen = isIpv4 ? 4 : 16;
        int answerLen = 2 + 2 + 2 + 4 + 2 + ipLen;

        int totalLen = HeaderSize + qLen + answerLen + addLength;
        var resp = GC.AllocateUninitializedArray<byte>(totalLen);
        Span<byte> span = resp;

        BinaryPrimitives.WriteUInt16BigEndian(span.Slice(0, 2), id);
        span[2] = flagsHi;
        span[3] = flagsLo;
        BinaryPrimitives.WriteUInt16BigEndian(span.Slice(4, 2), qdCount);
        BinaryPrimitives.WriteUInt16BigEndian(span.Slice(6, 2), 1);
        BinaryPrimitives.WriteUInt16BigEndian(span.Slice(8, 2), 0);
        BinaryPrimitives.WriteUInt16BigEndian(span.Slice(10, 2), arCount);

        int offset = HeaderSize;
        req.AsSpan(HeaderSize, qLen).CopyTo(span.Slice(offset));
        offset += qLen;

        span[offset++] = 0xC0;
        span[offset++] = 0x0C;

        BinaryPrimitives.WriteUInt16BigEndian(span.Slice(offset), qType);
        offset += 2;

        BinaryPrimitives.WriteUInt16BigEndian(span.Slice(offset), 1);
        offset += 2;

        BinaryPrimitives.WriteInt32BigEndian(span.Slice(offset), ttlSeconds);
        offset += 4;

        BinaryPrimitives.WriteUInt16BigEndian(span.Slice(offset), (ushort)ipLen);
        offset += 2;

        if (!ip.TryWriteBytes(span.Slice(offset, ipLen), out _))
        {
            throw new InvalidOperationException("Failed to write IP address bytes");
        }
        offset += ipLen;

        if (addLength > 0)
        {
            req.AsSpan(addStart, addLength).CopyTo(span.Slice(offset));
        }

        return resp;
    }

    public static byte[] AttachEdeOption(byte[] baseResponse, ushort edeCode)
    {
        Span<byte> edeSpan = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(edeSpan, edeCode);

        return AttachUpstreamEde(baseResponse, edeSpan);
    }

    public static byte[] AttachUpstreamEde(byte[] baseResponse, ReadOnlySpan<byte> upstreamEdeBytes)
    {
        if (baseResponse == null || baseResponse.Length < HeaderSize || upstreamEdeBytes.IsEmpty)
        {
            return baseResponse ?? Array.Empty<byte>();
        }

        ushort arCount = BinaryPrimitives.ReadUInt16BigEndian(baseResponse.AsSpan(10, 2));

        if (arCount > 0 && TryFindOptRecord(baseResponse, out int optHeaderOffset, out int rdataOffset, out ushort optRdLen))
        {
            int extraLen = 4 + upstreamEdeBytes.Length;
            byte[] finalResp = GC.AllocateUninitializedArray<byte>(baseResponse.Length + extraLen);

            int rdataEnd = rdataOffset + optRdLen;

            // Copy up to current RDATA end
            baseResponse.AsSpan(0, rdataEnd).CopyTo(finalResp);

            // Write new EDE Option at end of existing RDATA
            Span<byte> edeDataSpan = finalResp.AsSpan(rdataEnd);
            BinaryPrimitives.WriteUInt16BigEndian(edeDataSpan.Slice(0, 2), OptionCodeEdnsError);
            BinaryPrimitives.WriteUInt16BigEndian(edeDataSpan.Slice(2, 2), (ushort)upstreamEdeBytes.Length);
            upstreamEdeBytes.CopyTo(edeDataSpan.Slice(4));

            // Copy remaining packet bytes past current OPT RDATA if present
            if (baseResponse.Length > rdataEnd)
            {
                baseResponse.AsSpan(rdataEnd).CopyTo(finalResp.AsSpan(rdataEnd + extraLen));
            }

            // Update RDLENGTH field in the existing OPT record
            ushort newRdLen = (ushort)(optRdLen + extraLen);
            BinaryPrimitives.WriteUInt16BigEndian(finalResp.AsSpan(rdataOffset - 2, 2), newRdLen);

            return finalResp;
        }
        else
        {
            int optHeaderLen = 11;
            int optionHeaderLen = 4;
            int totalOptLen = optHeaderLen + optionHeaderLen + upstreamEdeBytes.Length;

            byte[] finalResp = GC.AllocateUninitializedArray<byte>(baseResponse.Length + totalOptLen);
            baseResponse.CopyTo(finalResp, 0);

            Span<byte> optSpan = finalResp.AsSpan(baseResponse.Length);

            optSpan[0] = 0x00;
            BinaryPrimitives.WriteUInt16BigEndian(optSpan.Slice(1, 2), 41);
            BinaryPrimitives.WriteUInt16BigEndian(optSpan.Slice(3, 2), 1232);
            BinaryPrimitives.WriteUInt32BigEndian(optSpan.Slice(5, 4), 0);

            ushort rdLength = (ushort)(optionHeaderLen + upstreamEdeBytes.Length);
            BinaryPrimitives.WriteUInt16BigEndian(optSpan.Slice(9, 2), rdLength);

            BinaryPrimitives.WriteUInt16BigEndian(optSpan.Slice(11, 2), OptionCodeEdnsError);
            BinaryPrimitives.WriteUInt16BigEndian(optSpan.Slice(13, 2), (ushort)upstreamEdeBytes.Length);

            upstreamEdeBytes.CopyTo(optSpan.Slice(15));

            BinaryPrimitives.WriteUInt16BigEndian(finalResp.AsSpan(10, 2), (ushort)(arCount + 1));

            return finalResp;
        }
    }

    private static bool TryFindOptRecord(ReadOnlySpan<byte> buffer, out int optHeaderOffset, out int rdataOffset, out ushort rdLen)
    {
        optHeaderOffset = 0;
        rdataOffset = 0;
        rdLen = 0;

        try
        {
            ushort arCount = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(10, 2));
            if (arCount == 0) return false;

            int offset = GetQuestionEnd(buffer);

            for (int i = 0; i < arCount; i++)
            {
                int recordStart = offset;
                offset = SkipName(buffer, offset);

                if (offset + 10 > buffer.Length) return false;

                ushort type = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(offset, 2));
                rdLen = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(offset + 8, 2));

                if (type == 41)
                {
                    optHeaderOffset = recordStart;
                    rdataOffset = offset + 10;
                    return true;
                }

                offset += 10 + rdLen;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    public static byte[] BuildServfail(byte[] req) => BuildRcodeResponse(req, 2);
}
