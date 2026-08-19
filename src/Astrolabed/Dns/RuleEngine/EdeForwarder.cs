using System;
using System.Buffers.Binary;

namespace Astrolabed.Dns.RuleEngine;

public static class EdeForwarder
{
    public static byte[] AttachUpstreamEde(byte[] destinationResponse, ReadOnlySpan<byte> edeOptionBytes)
    {
        ArgumentNullException.ThrowIfNull(destinationResponse);
        if (destinationResponse.Length < 12 || edeOptionBytes.IsEmpty)
        {
            return destinationResponse;
        }

        // EDNS OPT RR header overhead: Root Domain (1) + TYPE OPT (2) + UDP Payload Size (2) + ExtRCode/Flags (4) + RDLENGTH (2) = 11 bytes
        ushort rdLength = (ushort)edeOptionBytes.Length;
        int optRecordSize = 11 + rdLength;

        byte[] finalResponse = new byte[destinationResponse.Length + optRecordSize];
        destinationResponse.CopyTo(finalResponse, 0);

        int offset = destinationResponse.Length;

        // OPT Pseudo-Record Header
        finalResponse[offset++] = 0x00; // Root Domain
        BinaryPrimitives.WriteUInt16BigEndian(finalResponse.AsSpan(offset, 2), 41); // TYPE 41 (OPT)
        offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(finalResponse.AsSpan(offset, 2), 4096); // UDP payload size
        offset += 2;
        BinaryPrimitives.WriteUInt32BigEndian(finalResponse.AsSpan(offset, 4), 0); // Extended RCODE & Flags
        offset += 4;
        BinaryPrimitives.WriteUInt16BigEndian(finalResponse.AsSpan(offset, 2), rdLength); // RDLENGTH
        offset += 2;

        // EDE RDATA
        edeOptionBytes.CopyTo(finalResponse.AsSpan(offset));

        // Increment Additional Records Count (ARCOUNT) in DNS Header (Offset 10)
        ushort arCount = BinaryPrimitives.ReadUInt16BigEndian(finalResponse.AsSpan(10, 2));
        arCount++;
        BinaryPrimitives.WriteUInt16BigEndian(finalResponse.AsSpan(10, 2), arCount);

        return finalResponse;
    }
}
