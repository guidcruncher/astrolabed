using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Astrolabed.Dns.Core
{
    internal static class DnsResponseBuilder
    {
        private const int HeaderSize = 12;

        public static int GetQuestionEnd(byte[] req)
        {
            if (req == null || req.Length < HeaderSize)
                throw new ArgumentException("Request too short", nameof(req));

            ushort qdcount = BinaryPrimitives.ReadUInt16BigEndian(req.AsSpan(4, 2));
            int offset = HeaderSize;

            for (int q = 0; q < qdcount; q++)
            {
                while (true)
                {
                    if (offset >= req.Length)
                        throw new ArgumentException("Malformed DNS packet: label overruns packet", nameof(req));

                    byte len = req[offset++];
                    if (len == 0) break;

                    // pointer inside question not allowed per strict spec; handle defensively
                    if ((len & 0xC0) == 0xC0)
                    {
                        // pointer is two bytes total; we've consumed one, consume the second and stop
                        offset++;
                        break;
                    }

                    offset += len;
                    if (offset > req.Length)
                        throw new ArgumentException("Malformed DNS packet: label overruns packet", nameof(req));
                }

                // QTYPE + QCLASS = 4 bytes
                if (offset + 4 > req.Length)
                    throw new ArgumentException("Malformed DNS packet: missing QTYPE/QCLASS", nameof(req));

                offset += 4;
            }

            return offset;
        }

        private static int SkipName(byte[] buf, int offset)
        {
            int len = buf.Length;
            while (true)
            {
                if (offset >= len)
                    throw new ArgumentException("Malformed DNS packet while parsing name", nameof(buf));

                byte b = buf[offset++];
                if (b == 0)
                    break;

                // pointer: two-byte pointer, stops name
                if ((b & 0xC0) == 0xC0)
                {
                    // second byte must exist
                    if (offset >= len)
                        throw new ArgumentException("Malformed DNS packet: incomplete pointer", nameof(buf));
                    offset++;
                    break;
                }

                // label length byte - advance by that amount
                offset += b;
                if (offset > len)
                    throw new ArgumentException("Malformed DNS packet: label overruns packet", nameof(buf));
            }

            return offset;
        }

        private static (byte[] bytes, ushort count) ExtractAdditionalRecords(byte[] req)
        {
            try
            {
                if (req == null || req.Length < HeaderSize)
                    return (Array.Empty<byte>(), 0);

                ushort arCount = BinaryPrimitives.ReadUInt16BigEndian(req.AsSpan(10, 2));
                if (arCount == 0)
                    return (Array.Empty<byte>(), 0);

                int qEnd = GetQuestionEnd(req);
                int offset = qEnd;
                var parts = new List<byte[]>();

                for (int i = 0; i < arCount; i++)
                {
                    int nameStart = offset;
                    offset = SkipName(req, offset);

                    // need at least TYPE(2) + CLASS(2) + TTL(4) + RDLENGTH(2)
                    if (offset + 10 > req.Length)
                        throw new ArgumentException("Malformed DNS packet: additional RR header too short", nameof(req));

                    ushort rdlen = BinaryPrimitives.ReadUInt16BigEndian(req.AsSpan(offset + 8, 2));

                    int rrTotalLen = (offset + 10 + rdlen) - nameStart;
                    if (nameStart + rrTotalLen > req.Length)
                        throw new ArgumentException("Malformed DNS packet: additional RR data overruns packet", nameof(req));

                    var rr = new byte[rrTotalLen];
                    Array.Copy(req, nameStart, rr, 0, rrTotalLen);
                    parts.Add(rr);

                    offset = offset + 10 + rdlen;
                }

                // concatenate
                int total = 0;
                foreach (var p in parts) total += p.Length;
                var outBuf = new byte[total];
                int pos = 0;
                foreach (var p in parts)
                {
                    Array.Copy(p, 0, outBuf, pos, p.Length);
                    pos += p.Length;
                }

                return (outBuf, (ushort)parts.Count);
            }
            catch
            {
                // On any parse error, return none — safer than echoing malformed data
                return (Array.Empty<byte>(), 0);
            }
        }

        public static byte[] CopyQuestionBytes(byte[] req)
        {
            int end = GetQuestionEnd(req);
            int len = end - HeaderSize;
            var outBytes = new byte[len];
            Array.Copy(req, HeaderSize, outBytes, 0, len);
            return outBytes;
        }

        private static byte[] BuildHeader(ushort id, byte flagsHi, byte flagsLo, ushort qdCount, ushort anCount, ushort nsCount = 0, ushort arCount = 0)
        {
            var header = new byte[HeaderSize];
            BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(0, 2), id);
            header[2] = flagsHi;
            header[3] = flagsLo;
            BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(4, 2), qdCount);
            BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(6, 2), anCount);
            BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(8, 2), nsCount);
            BinaryPrimitives.WriteUInt16BigEndian(header.AsSpan(10, 2), arCount);
            return header;
        }

        public static byte[] BuildRcodeResponse(byte[] req, int rcode)
        {
            ushort id = BinaryPrimitives.ReadUInt16BigEndian(req.AsSpan(0, 2));
            byte reqFlagsHi = req[2];
            byte flagsHi = (byte)((reqFlagsHi & 0x01) | 0x80); // keep RD, set QR
            byte flagsLo = (byte)(0x80 | (rcode & 0x0F)); // RA=1 + rcode

            var qBytes = CopyQuestionBytes(req);
            ushort qdCount = BinaryPrimitives.ReadUInt16BigEndian(req.AsSpan(4, 2));

            var add = ExtractAdditionalRecords(req);

            var header = BuildHeader(id, flagsHi, flagsLo, qdCount, 0, 0, add.count);

            var resp = new List<byte>(HeaderSize + qBytes.Length + add.bytes.Length);
            resp.AddRange(header);
            resp.AddRange(qBytes);

            if (add.count > 0)
                resp.AddRange(add.bytes);

            return resp.ToArray();
        }

        public static byte[] BuildStaticIpResponse(byte[] req, IPAddress ip, int ttlSeconds = 60)
        {
            ushort id = BinaryPrimitives.ReadUInt16BigEndian(req.AsSpan(0, 2));
            byte reqFlagsHi = req[2];
            byte flagsHi = (byte)((reqFlagsHi & 0x01) | 0x80); // keep RD, set QR
            byte flagsLo = 0x80; // RA=1, RCODE=0

            var qBytes = CopyQuestionBytes(req);
            ushort qdCount = BinaryPrimitives.ReadUInt16BigEndian(req.AsSpan(4, 2));
            ushort anCount = 1;

            var add = ExtractAdditionalRecords(req);

            var header = BuildHeader(id, flagsHi, flagsLo, qdCount, anCount, 0, add.count);

            var outBytes = new List<byte>(HeaderSize + qBytes.Length + 16 + add.bytes.Length);
            outBytes.AddRange(header);
            outBytes.AddRange(qBytes);

            // NAME: pointer to offset 12 (0xC00C)
            outBytes.Add(0xC0);
            outBytes.Add(0x0C);

            // TYPE
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                outBytes.AddRange(new byte[] { 0x00, 0x01 }); // A
            else
                outBytes.AddRange(new byte[] { 0x00, 0x1C }); // AAAA

            // CLASS: IN
            outBytes.AddRange(new byte[] { 0x00, 0x01 });

            // TTL
            var ttl = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(ttl, ttlSeconds);
            outBytes.AddRange(ttl);

            // RDLENGTH (big-endian) + RDATA
            var addrBytes = ip.GetAddressBytes();
            outBytes.Add((byte)((addrBytes.Length >> 8) & 0xFF));
            outBytes.Add((byte)(addrBytes.Length & 0xFF));
            outBytes.AddRange(addrBytes);

            if (add.count > 0)
                outBytes.AddRange(add.bytes);

            return outBytes.ToArray();
        }

        public static byte[] BuildServfail(byte[] req) => BuildRcodeResponse(req, 2);
    }
}
