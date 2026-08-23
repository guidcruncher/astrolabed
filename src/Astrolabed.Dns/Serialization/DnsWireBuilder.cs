// File: src/Astrolabed.Dns/Serialization/DnsWireBuilder.cs
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;

using Astrolabed.Dns.Models;

namespace Astrolabed.Dns.Serialization;

/// <summary>
/// Provides high-performance binary serialization for DNS wire-format messages compliant with RFC 1035 and EDNS0 (RFC 6891 / RFC 8914).
/// </summary>
public static class DnsWireBuilder
{
    private const ushort DefaultClassIn = 1;
    private const ushort EdnsOptType = 41;
    private const ushort EdnsEdeOptionCode = 15;
    private const ushort CompressionMask = 0xC000;
    private const ushort MaxCompressionOffset = 0x3FFF;

    /// <summary>
    /// Builds a serialized DNS wire-format response payload.
    /// </summary>
    /// <param name="request">The original request DNS message.</param>
    /// <param name="responseCode">The response code to assign.</param>
    /// <param name="answers">Optional resource records for the Answer section.</param>
    /// <param name="ede">Optional Extended DNS Error information for EDNS0 OPT RR.</param>
    /// <returns>A byte array containing the full DNS wire-format response.</returns>
    public static byte[] BuildResponse(
        DnsWireMessage request,
        DnsResponseCode responseCode,
        IEnumerable<DnsResourceRecord>? answers = null,
        ExtendedDnsError? ede = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var writer = new ArrayBufferWriter<byte>(512);
        var compressionMap = new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase);

        IReadOnlyList<DnsResourceRecord> answerList = answers switch
        {
            IReadOnlyList<DnsResourceRecord> list => list,
            IEnumerable<DnsResourceRecord> enumable => enumable.ToList(),
            _ => Array.Empty<DnsResourceRecord>()
        };

        // 1. Render Header (12 bytes)
        Span<byte> header = writer.GetSpan(12)[..12];

        BinaryPrimitives.WriteUInt16BigEndian(header[0..2], request.TransactionId);

        // Flags: QR=1, Opcode=0, AA=1, RD=1, RA=1, RCODE
        ushort flags = 0x8180;
        flags |= (ushort)((byte)responseCode & 0x0F);
        BinaryPrimitives.WriteUInt16BigEndian(header[2..4], flags);

        // Counts: QDCOUNT=1, ANCOUNT, NSCOUNT=0, ARCOUNT (1 if EDNS EDE present)
        BinaryPrimitives.WriteUInt16BigEndian(header[4..6], 1);
        BinaryPrimitives.WriteUInt16BigEndian(header[6..8], (ushort)answerList.Count);
        BinaryPrimitives.WriteUInt16BigEndian(header[8..10], 0);
        BinaryPrimitives.WriteUInt16BigEndian(header[10..12], ede is not null ? (ushort)1 : (ushort)0);

        writer.Advance(12);

        // 2. Question Section
        WriteDomainName(writer, request.QuestionName, compressionMap);

        Span<byte> qTypeClass = writer.GetSpan(4)[..4];
        BinaryPrimitives.WriteUInt16BigEndian(qTypeClass[0..2], (ushort)request.QuestionType);
        BinaryPrimitives.WriteUInt16BigEndian(qTypeClass[2..4], DefaultClassIn);
        writer.Advance(4);

        // 3. Answer Section
        foreach (DnsResourceRecord rr in answerList)
        {
            EncodeResourceRecord(writer, rr, compressionMap);
        }

        // 4. Additional Section (EDNS0 OPT RR with Extended DNS Error)
        if (ede is not null)
        {
            EncodeEdnsOption(writer, ede);
        }

        return writer.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Encodes a domain name into a pre-allocated byte array buffer at a specific offset.
    /// </summary>
    /// <param name="buffer">Target buffer array.</param>
    /// <param name="offset">Current buffer offset, updated upon completion.</param>
    /// <param name="domain">Domain name string to encode.</param>
    public static void EncodeDomainName(byte[] buffer, ref int offset, string domain)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        EncodeDomainName(buffer.AsSpan(), ref offset, domain);
    }

    /// <summary>
    /// Encodes a domain name into a span buffer at a specific offset.
    /// </summary>
    /// <param name="buffer">Target span buffer.</param>
    /// <param name="offset">Current buffer offset, updated upon completion.</param>
    /// <param name="domain">Domain name string to encode.</param>
    public static void EncodeDomainName(Span<byte> buffer, ref int offset, string domain)
    {
        var writer = new ArrayBufferWriter<byte>(128);
        var dummyMap = new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase);
        WriteDomainName(writer, domain, dummyMap);

        ReadOnlySpan<byte> written = writer.WrittenSpan;
        written.CopyTo(buffer[offset..]);
        offset += written.Length;
    }

    /// <summary>
    /// Writes an RFC 1035 domain name with support for dictionary label compression.
    /// </summary>
    /// <param name="writer">Target buffer writer.</param>
    /// <param name="domain">Domain name string.</param>
    /// <param name="compressionMap">Map tracking suffix offsets for DNS compression.</param>
    public static void WriteDomainName(
        IBufferWriter<byte> writer,
        string domain,
        Dictionary<string, ushort> compressionMap)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(compressionMap);

        if (string.IsNullOrEmpty(domain) || domain == ".")
        {
            Span<byte> nullLabel = writer.GetSpan(1)[..1];
            nullLabel[0] = 0;
            writer.Advance(1);
            return;
        }

        ReadOnlySpan<char> domainSpan = domain.AsSpan().TrimEnd('.');
        int totalLength = domainSpan.Length;
        int currentOffset = 0;

        while (currentOffset < totalLength)
        {
            string currentSuffix = domainSpan[currentOffset..].ToString();

            if (compressionMap.TryGetValue(currentSuffix, out ushort pointerOffset) && pointerOffset <= MaxCompressionOffset)
            {
                Span<byte> pointerBytes = writer.GetSpan(2)[..2];
                ushort pointer = (ushort)(CompressionMask | pointerOffset);
                BinaryPrimitives.WriteUInt16BigEndian(pointerBytes, pointer);
                writer.Advance(2);
                return;
            }

            // Record current writer position as compression target if within max offset limit
            if (writer is ArrayBufferWriter<byte> bufferWriter && bufferWriter.WrittenCount <= MaxCompressionOffset)
            {
                compressionMap[currentSuffix] = (ushort)bufferWriter.WrittenCount;
            }

            int nextDot = domainSpan[currentOffset..].IndexOf('.');
            ReadOnlySpan<char> label = nextDot < 0
                ? domainSpan[currentOffset..]
                : domainSpan.Slice(currentOffset, nextDot);

            int byteCount = Encoding.ASCII.GetByteCount(label);
            if (byteCount is 0 or > 63)
            {
                throw new ArgumentException($"DNS label '{label.ToString()}' length must be between 1 and 63 bytes.");
            }

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

        Span<byte> terminator = writer.GetSpan(1)[..1];
        terminator[0] = 0;
        writer.Advance(1);
    }

    private static void EncodeResourceRecord(
        IBufferWriter<byte> writer,
        DnsResourceRecord rr,
        Dictionary<string, ushort> compressionMap)
    {
        // Owner Name
        WriteDomainName(writer, rr.Name, compressionMap);

        // RR Header (Type, Class, TTL)
        Span<byte> rrHeader = writer.GetSpan(10)[..10];
        BinaryPrimitives.WriteUInt16BigEndian(rrHeader[0..2], (ushort)rr.Type);
        BinaryPrimitives.WriteUInt16BigEndian(rrHeader[2..4], rr.Class == 0 ? DefaultClassIn : rr.Class);
        BinaryPrimitives.WriteUInt32BigEndian(rrHeader[4..8], (uint)rr.Ttl);

        // Reserve 2 bytes for RDLENGTH (written at offset 8..10)
        writer.Advance(10);

        int rdataStartOffset = writer is ArrayBufferWriter<byte> abw ? abw.WrittenCount : 0;

        // Encode RDATA Payload
        if (rr.ParsedIp is not null)
        {
            bool success = rr.ParsedIp.TryWriteBytes(writer.GetSpan(16), out int bytesWritten);
            if (success)
            {
                writer.Advance(bytesWritten);
            }
        }
        else if (IsDomainTargetRecord(rr.Type))
        {
            string? targetDomain = ExtractDomainString(rr);
            if (!string.IsNullOrEmpty(targetDomain))
            {
                WriteDomainName(writer, targetDomain, compressionMap);
            }
            else if (rr.Data is not null)
            {
                writer.Write(rr.Data);
            }
        }
        else if (rr.Data is not null)
        {
            writer.Write(rr.Data);
        }

        // Backfill actual RDATA Length into reserved header bytes
        if (writer is ArrayBufferWriter<byte> arrayWriter)
        {
            int rdataLength = arrayWriter.WrittenCount - rdataStartOffset;
            Span<byte> writtenSpan = MemoryMarshal.CreateSpan(
                ref MemoryMarshal.GetReference(arrayWriter.WrittenSpan),
                arrayWriter.WrittenCount);

            BinaryPrimitives.WriteUInt16BigEndian(writtenSpan.Slice(rdataStartOffset - 2, 2), (ushort)rdataLength);
        }
    }

    private static bool IsDomainTargetRecord(DnsType type) =>
        type is DnsType.CNAME or DnsType.NS or DnsType.PTR or DnsType.DNAME;

    private static string? ExtractDomainString(DnsResourceRecord rr)
    {
        if (rr.Data is null || rr.Data.Length == 0)
        {
            return null;
        }

        int offset = 0;
        if (DnsWireParser.TryReadDomainName(rr.Data, ref offset, out string? domain) && !string.IsNullOrEmpty(domain))
        {
            return domain;
        }

        return null;
    }

    private static void EncodeEdnsOption(IBufferWriter<byte> writer, ExtendedDnsError ede)
    {
        // Root Domain Name "."
        Span<byte> rootName = writer.GetSpan(1)[..1];
        rootName[0] = 0;
        writer.Advance(1);

        int extraTextByteCount = string.IsNullOrEmpty(ede.ExtraText) ? 0 : Encoding.UTF8.GetByteCount(ede.ExtraText);
        ushort optionDataLength = (ushort)(2 + extraTextByteCount);
        ushort totalRdataLength = (ushort)(4 + optionDataLength);

        // OPT RR Header: Type=41, UDPPayload=4096, TTL/RCODE=0, RDLENGTH
        Span<byte> optHeader = writer.GetSpan(10)[..10];
        BinaryPrimitives.WriteUInt16BigEndian(optHeader[0..2], EdnsOptType);
        BinaryPrimitives.WriteUInt16BigEndian(optHeader[2..4], 4096);
        BinaryPrimitives.WriteUInt32BigEndian(optHeader[4..8], 0);
        BinaryPrimitives.WriteUInt16BigEndian(optHeader[8..10], totalRdataLength);
        writer.Advance(10);

        // Option Header: Code=15 (EDE), OptionLength
        Span<byte> optionHeader = writer.GetSpan(4)[..4];
        BinaryPrimitives.WriteUInt16BigEndian(optionHeader[0..2], EdnsEdeOptionCode);
        BinaryPrimitives.WriteUInt16BigEndian(optionHeader[2..4], optionDataLength);
        writer.Advance(4);

        // EDE InfoCode (2 bytes)
        Span<byte> infoCodeSpan = writer.GetSpan(2)[..2];
        BinaryPrimitives.WriteUInt16BigEndian(infoCodeSpan, (ushort)ede.InfoCode);
        writer.Advance(2);

        // EDE ExtraText payload if provided
        if (extraTextByteCount > 0)
        {
            Span<byte> extraTextSpan = writer.GetSpan(extraTextByteCount)[..extraTextByteCount];
            Encoding.UTF8.GetBytes(ede.ExtraText, extraTextSpan);
            writer.Advance(extraTextByteCount);
        }
    }
}
