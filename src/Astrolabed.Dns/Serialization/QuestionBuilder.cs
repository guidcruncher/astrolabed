// File: src/Astrolabed.Dns/Serialization/QuestionBuilder.cs
using System.Buffers.Binary;

using Astrolabed.Dns.Models;

namespace Astrolabed.Dns.Serialization;

public static class QuestionBuilder
{
    public static byte[] BuildQuery(
        string domainName,
        DnsType type = DnsType.A,
        ushort? transactionId = null,
        bool recursionDesired = true)
    {
        Span<byte> header = stackalloc byte[12];

        // 1. Transaction ID (generate random ID if not provided)
        ushort txId = transactionId ?? (ushort)Random.Shared.Next(1, 65535);
        BinaryPrimitives.WriteUInt16BigEndian(header[0..2], txId);

        // 2. Query Flags: QR=0 (Query), Opcode=0, RD=1 (Recursion Desired) or 0
        ushort flags = recursionDesired ? (ushort)0x0100 : (ushort)0x0000;
        BinaryPrimitives.WriteUInt16BigEndian(header[2..4], flags);

        // 3. Header Section Counts: QDCOUNT=1, ANCOUNT=0, NSCOUNT=0, ARCOUNT=0
        BinaryPrimitives.WriteUInt16BigEndian(header[4..6], 1);
        BinaryPrimitives.WriteUInt16BigEndian(header[6..8], 0);
        BinaryPrimitives.WriteUInt16BigEndian(header[8..10], 0);
        BinaryPrimitives.WriteUInt16BigEndian(header[10..12], 0);

        var buffer = new List<byte>(256);
        buffer.AddRange(header);

        // 4. Encode Question Section Domain Name
        var compressionMap = new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase);
        DnsWireBuilder.WriteDomainName(buffer, domainName, compressionMap);

        // 5. Encode Question Type and Class (IN = 1)
        byte[] typeAndClass = new byte[4];
        BinaryPrimitives.WriteUInt16BigEndian(typeAndClass.AsSpan(0, 2), (ushort)type);
        BinaryPrimitives.WriteUInt16BigEndian(typeAndClass.AsSpan(2, 2), 1); // Class IN
        buffer.AddRange(typeAndClass);

        return buffer.ToArray();
    }

    public static byte[] BuildPtrQuery(string ptrDomain, ushort? transactionId = null)
    {
        return BuildQuery(ptrDomain, DnsType.PTR, transactionId, recursionDesired: true);
    }
}
