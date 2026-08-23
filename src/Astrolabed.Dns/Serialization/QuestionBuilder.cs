using System.Buffers;
using System.Buffers.Binary;
using System.Security.Cryptography;

using Astrolabed.Dns.Models;

namespace Astrolabed.Dns.Serialization;

/// <summary>
/// Provides zero-allocation, high-performance serialization for building outgoing RFC 1035 DNS queries.
/// </summary>
public static class QuestionBuilder
{
    private const ushort DefaultClassIn = 1;

    /// <summary>
    /// Constructs a binary RFC 1035 DNS query payload.
    /// </summary>
    /// <param name="domainName">Target domain name to query.</param>
    /// <param name="type">DNS record type requested.</param>
    /// <param name="transactionId">Optional explicit transaction ID. If null, a cryptographically secure ID is generated.</param>
    /// <param name="recursionDesired">Specifies whether the Recursion Desired (RD) flag should be set.</param>
    /// <returns>A byte array containing the serialized binary DNS query.</returns>
    public static byte[] BuildQuery(
        string domainName,
        DnsType type = DnsType.A,
        ushort? transactionId = null,
        bool recursionDesired = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(domainName);

        var writer = new ArrayBufferWriter<byte>(256);

        // 1. Generate or assign Cryptographically Secure Transaction ID
        ushort txId = transactionId ?? (ushort)RandomNumberGenerator.GetInt32(1, 65536);

        // 2. Build 12-byte Header
        Span<byte> header = writer.GetSpan(12)[..12];

        BinaryPrimitives.WriteUInt16BigEndian(header[0..2], txId);

        // Query Flags: QR=0 (Query), Opcode=0, RD (Recursion Desired)
        ushort flags = recursionDesired ? (ushort)0x0100 : (ushort)0x0000;
        BinaryPrimitives.WriteUInt16BigEndian(header[2..4], flags);

        // Counts: QDCOUNT=1, ANCOUNT=0, NSCOUNT=0, ARCOUNT=0
        BinaryPrimitives.WriteUInt16BigEndian(header[4..6], 1);
        BinaryPrimitives.WriteUInt16BigEndian(header[6..8], 0);
        BinaryPrimitives.WriteUInt16BigEndian(header[8..10], 0);
        BinaryPrimitives.WriteUInt16BigEndian(header[10..12], 0);

        writer.Advance(12);

        // 3. Write Question Section Domain Name
        var compressionMap = new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase);
        DnsWireBuilder.WriteDomainName(writer, domainName, compressionMap);

        // 4. Write Question Type and Class (IN = 1)
        Span<byte> qTypeClass = writer.GetSpan(4)[..4];
        BinaryPrimitives.WriteUInt16BigEndian(qTypeClass[0..2], (ushort)type);
        BinaryPrimitives.WriteUInt16BigEndian(qTypeClass[2..4], DefaultClassIn);
        writer.Advance(4);

        return writer.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Constructs a PTR reverse lookup query payload.
    /// </summary>
    /// <param name="ptrDomain">Target reverse DNS domain (e.g., "1.0.0.127.in-addr.arpa").</param>
    /// <param name="transactionId">Optional explicit transaction ID.</param>
    /// <returns>A byte array containing the serialized binary PTR DNS query.</returns>
    public static byte[] BuildPtrQuery(string ptrDomain, ushort? transactionId = null)
    {
        return BuildQuery(ptrDomain, DnsType.PTR, transactionId, recursionDesired: true);
    }
}
