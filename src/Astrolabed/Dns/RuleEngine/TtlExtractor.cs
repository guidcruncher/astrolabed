using System;
using System.Buffers.Binary;

namespace Astrolabed.Dns.RuleEngine;

internal static class TtlExtractor
{
    public static int ExtractTtl(ReadOnlySpan<byte> msg)
    {
        if (msg.Length < 12)
        {
            return -1;
        }

        ushort qd = BinaryPrimitives.ReadUInt16BigEndian(msg.Slice(4, 2));
        ushort an = BinaryPrimitives.ReadUInt16BigEndian(msg.Slice(6, 2));

        int offset = 12;

        for (int i = 0; i < qd; i++)
        {
            offset = SkipName(msg, offset);
            if (offset < 0 || offset + 4 > msg.Length)
            {
                return -1;
            }

            offset += 4;
        }

        uint min = uint.MaxValue;

        for (int i = 0; i < an; i++)
        {
            offset = SkipName(msg, offset);
            if (offset < 0 || offset + 10 > msg.Length)
            {
                return -1;
            }

            offset += 4; // Skip Type and Class

            uint ttl = BinaryPrimitives.ReadUInt32BigEndian(msg.Slice(offset, 4));
            offset += 4;

            ushort rdLength = BinaryPrimitives.ReadUInt16BigEndian(msg.Slice(offset, 2));
            offset += 2 + rdLength;

            if (offset > msg.Length)
            {
                return -1;
            }

            if (ttl < min)
            {
                min = ttl;
            }
        }

        return min == uint.MaxValue ? -1 : (int)Math.Min(min, int.MaxValue);
    }

    private static int SkipName(ReadOnlySpan<byte> msg, int offset)
    {
        int hops = 0;
        while (offset < msg.Length)
        {
            byte len = msg[offset];

            if (len == 0)
            {
                return offset + 1;
            }

            if ((len & 0xC0) == 0xC0)
            {
                return offset + 2;
            }

            offset += len + 1;

            if (++hops > 128)
            {
                return -1;
            }
        }

        return -1;
    }
}
