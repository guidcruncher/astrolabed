using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Dns;
using Astrolabed.Dns.Core;

using Microsoft.Extensions.Logging;

namespace Astrolabed.Api.Services;

public sealed class DnsRequestHandler : IDnsRequestHandler
{
    private readonly ILogger<DnsRequestHandler> _logger;
    private readonly Astrolabed.Dns.RuleEngine.RuleEngine _ruleEngine;

    public DnsRequestHandler(
        ILogger<DnsRequestHandler> logger,
        Astrolabed.Dns.RuleEngine.RuleEngine ruleEngine)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(ruleEngine);

        _logger = logger;
        _ruleEngine = ruleEngine;
    }

    public async Task<DnsHandlerResult> HandleAsync(
        byte[] request,
        IPEndPoint remote,
        CancellationToken cancellationToken = default)
    {
        using var pooledBuffer = await ProcessAsync(request, remote, cancellationToken).ConfigureAwait(false);

        if (pooledBuffer is null || pooledBuffer.Length == 0)
        {
            return CreateErrorResult("SERVFAIL", request ?? Array.Empty<byte>());
        }

        var responseBytes = pooledBuffer.Span.ToArray();
        return ParseDnsResponse(responseBytes);
    }

    public async Task<PooledBuffer?> ProcessAsync(
        byte[] request,
        IPEndPoint remote,
        CancellationToken cancellationToken = default)
    {
        if (request == null || request.Length < 12)
        {
            return null;
        }

        string requestId = _logger.IsEnabled(LogLevel.Debug)
            ? Guid.CreateVersion7().ToString("N")
            : string.Empty;

        var context = new DnsRequestContext(request, requestId, "127.0.0.1", "localhost");

        if (string.IsNullOrEmpty(context.Domain))
        {
            if (!string.IsNullOrEmpty(requestId))
            {
                _logger.LogWarning("Request {RequestId}: Received DNS message with no questions from {Remote}", requestId, remote);
            }
            return null;
        }

        var response = await _ruleEngine.QueryAsync(context, cancellationToken).ConfigureAwait(false);

        if (response is null || response.Length == 0)
        {
            return null;
        }

        return ApplyTruncationAndBuffer(response, request);
    }

    private static PooledBuffer ApplyTruncationAndBuffer(byte[] response, byte[] request)
    {
        int maxPayloadSize = ExtractEdns0PayloadSize(request);

        if (response.Length <= maxPayloadSize)
        {
            var poolBuffer = ArrayPool<byte>.Shared.Rent(response.Length);
            Buffer.BlockCopy(response, 0, poolBuffer, 0, response.Length);
            return new PooledBuffer(poolBuffer, response.Length, fromPool: true);
        }

        int offset = 12;
        while (offset < response.Length)
        {
            byte len = response[offset];
            if (len == 0)
            {
                offset += 5;
                break;
            }
            if ((len & 0xC0) == 0xC0)
            {
                offset += 6;
                break;
            }
            offset += len + 1;
        }

        if (offset > response.Length)
        {
            offset = 12;
        }

        var truncatedPoolBuffer = ArrayPool<byte>.Shared.Rent(offset);
        Buffer.BlockCopy(response, 0, truncatedPoolBuffer, 0, offset);

        truncatedPoolBuffer[2] |= 0x02; // Set TC bit
        truncatedPoolBuffer.AsSpan(6, 6).Clear(); // Clear section counts

        return new PooledBuffer(truncatedPoolBuffer, offset, fromPool: true);
    }

    private static int ExtractEdns0PayloadSize(byte[] request)
    {
        if (request == null || request.Length < 12) return 512;

        ushort qdCount = BinaryPrimitives.ReadUInt16BigEndian(request.AsSpan(4, 2));
        ushort anCount = BinaryPrimitives.ReadUInt16BigEndian(request.AsSpan(6, 2));
        ushort nsCount = BinaryPrimitives.ReadUInt16BigEndian(request.AsSpan(8, 2));
        ushort arCount = BinaryPrimitives.ReadUInt16BigEndian(request.AsSpan(10, 2));

        if (arCount == 0) return 512;

        int offset = 12;

        // Skip Question Section
        for (int i = 0; i < qdCount; i++)
        {
            offset = SkipDomainName(request, offset);
            if (offset < 0 || offset + 4 > request.Length) return 512;
            offset += 4; // Type + Class
        }

        // Skip Answer Section
        if (!SkipResourceRecords(request, ref offset, anCount)) return 512;

        // Skip Authority Section
        if (!SkipResourceRecords(request, ref offset, nsCount)) return 512;

        // Search Additional Section specifically for OPT Record (TYPE 41)
        for (int i = 0; i < arCount; i++)
        {
            offset = SkipDomainName(request, offset);
            if (offset < 0 || offset + 10 > request.Length) return 512;

            ushort type = BinaryPrimitives.ReadUInt16BigEndian(request.AsSpan(offset, 2));
            ushort udpSize = BinaryPrimitives.ReadUInt16BigEndian(request.AsSpan(offset + 2, 2));
            ushort rdLength = BinaryPrimitives.ReadUInt16BigEndian(request.AsSpan(offset + 8, 2));

            if (type == 41) // OPT record
            {
                return Math.Max(512, (int)udpSize);
            }

            offset += 10 + rdLength;
        }

        return 512;
    }

    private static bool SkipResourceRecords(byte[] buffer, ref int offset, int count)
    {
        for (int i = 0; i < count; i++)
        {
            offset = SkipDomainName(buffer, offset);
            if (offset < 0 || offset + 10 > buffer.Length) return false;

            ushort rdLength = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset + 8, 2));
            offset += 10 + rdLength;
            if (offset > buffer.Length) return false;
        }

        return true;
    }

    private static int SkipDomainName(byte[] buffer, int offset)
    {
        int pointerJumps = 0;
        while (offset < buffer.Length)
        {
            byte len = buffer[offset];
            if (len == 0) return offset + 1;
            if ((len & 0xC0) == 0xC0)
            {
                if (offset + 2 > buffer.Length) return -1;
                return offset + 2; // Compression Pointer (2 bytes)
            }
            offset += len + 1;
            if (++pointerJumps > 128) return -1; // Circuit break circular loops
        }
        return -1;
    }

    private static DnsHandlerResult ParseDnsResponse(byte[] buffer)
    {
        if (buffer == null || buffer.Length < 12)
        {
            return CreateErrorResult("FORMERR", buffer ?? Array.Empty<byte>());
        }

        ushort flags = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(2, 2));
        int rcode = flags & 0x0F;
        string rcodeName = MapRcodeToString(rcode);

        var answers = new List<DnsResourceRecord>();
        var authorities = new List<DnsResourceRecord>();
        var additionals = new List<DnsResourceRecord>();

        ushort qdCount = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(4, 2));
        ushort anCount = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(6, 2));
        ushort nsCount = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(8, 2));
        ushort arCount = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(10, 2));

        int offset = 12;

        // Skip Question Section
        for (int i = 0; i < qdCount && offset < buffer.Length; i++)
        {
            offset = SkipQuestionSection(buffer, offset);
        }

        // Parse Answer RRs
        for (int i = 0; i < anCount && offset < buffer.Length; i++)
        {
            if (TryParseResourceRecord(buffer, ref offset, out var record))
            {
                answers.Add(record);
            }
            else
            {
                break;
            }
        }

        // Parse Authority RRs
        for (int i = 0; i < nsCount && offset < buffer.Length; i++)
        {
            if (TryParseResourceRecord(buffer, ref offset, out var record))
            {
                authorities.Add(record);
            }
            else
            {
                break;
            }
        }

        // Parse Additional RRs
        for (int i = 0; i < arCount && offset < buffer.Length; i++)
        {
            if (TryParseResourceRecord(buffer, ref offset, out var record))
            {
                additionals.Add(record);
            }
            else
            {
                break;
            }
        }

        return new DnsHandlerResult
        {
            Success = rcode == 0,
            ResponseCode = rcodeName,
            Bytes = buffer,
            Answers = answers,
            Authorities = authorities,
            Additionals = additionals
        };
    }

    private static int SkipQuestionSection(byte[] buffer, int offset)
    {
        int end = SkipDomainName(buffer, offset);
        if (end < 0 || end + 4 > buffer.Length) return buffer.Length;
        return end + 4; // Skip QTYPE + QCLASS
    }

    private static bool TryParseResourceRecord(byte[] buffer, ref int offset, out DnsResourceRecord record)
    {
        record = null!;
        if (offset >= buffer.Length) return false;

        if (!TryReadDomainName(buffer, ref offset, out string name))
        {
            return false;
        }

        if (offset + 10 > buffer.Length) return false;

        ushort typeCode = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset, 2));
        ushort classCode = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset + 2, 2));
        uint ttl = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(offset + 4, 4));
        ushort rdLength = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset + 8, 2));

        offset += 10;

        // Strict boundary check before reading RDATA
        if (offset > buffer.Length || rdLength > (buffer.Length - offset))
        {
            return false;
        }

        string data = ParseRecordData(buffer, offset, typeCode, rdLength);
        offset += rdLength;

        record = new DnsResourceRecord
        {
            Name = name,
            Type = MapTypeCodeToString(typeCode),
            Class = typeCode == 41 ? "NONE" : (classCode == 1 ? "IN" : classCode.ToString()),
            TimeToLive = ttl,
            Data = data
        };

        return true;
    }

    private static bool TryReadDomainName(byte[] buffer, ref int offset, out string domain)
    {
        domain = string.Empty;
        var sb = new StringBuilder();
        int current = offset;
        bool jumped = false;
        int originalOffset = offset;
        int jumps = 0;

        while (current < buffer.Length)
        {
            byte len = buffer[current];
            if (len == 0)
            {
                if (!jumped) originalOffset = current + 1;
                domain = sb.Length == 0 ? "." : sb.ToString().TrimEnd('.');
                offset = originalOffset;
                return true;
            }

            if ((len & 0xC0) == 0xC0)
            {
                if (current + 1 >= buffer.Length) return false;
                if (!jumped)
                {
                    originalOffset = current + 2;
                    jumped = true;
                }
                current = ((len & 0x3F) << 8) | buffer[current + 1];
                if (++jumps > 10) return false; // Prevent circular compression pointers
                continue;
            }

            current++;
            if (current + len > buffer.Length) return false;

            sb.Append(Encoding.ASCII.GetString(buffer, current, len)).Append('.');
            current += len;
        }

        return false;
    }

    private static string ParseRecordData(byte[] buffer, int offset, ushort typeCode, ushort rdLength)
    {
        if (buffer == null || offset < 0 || rdLength < 0 || offset > buffer.Length || (offset + rdLength) > buffer.Length)
        {
            return string.Empty;
        }

        try
        {
            switch (typeCode)
            {
                case 1 when rdLength == 4:
                    return new IPAddress(buffer.AsSpan(offset, 4)).ToString();

                case 28 when rdLength == 16:
                    return new IPAddress(buffer.AsSpan(offset, 16)).ToString();

                case 2:  // NS
                case 5:  // CNAME
                case 12: // PTR
                    {
                        int ptr = offset;
                        if (TryReadDomainName(buffer, ref ptr, out var domain))
                        {
                            return domain;
                        }
                        break;
                    }

                case 16: // TXT
                    {
                        if (rdLength > 0)
                        {
                            int curr = offset;
                            int end = offset + rdLength;
                            var sb = new StringBuilder();
                            while (curr < end)
                            {
                                byte strLen = buffer[curr++];
                                if (curr + strLen > end) break;
                                sb.Append('"').Append(Encoding.UTF8.GetString(buffer, curr, strLen)).Append("\" ");
                                curr += strLen;
                            }
                            return sb.ToString().TrimEnd();
                        }
                        break;
                    }

                case 41: // OPT Record
                    return $"PayloadSize: {BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset - 8, 2))}, RDLength: {rdLength}";
            }

            return Convert.ToHexString(buffer.AsSpan(offset, rdLength));
        }
        catch
        {
            return Convert.ToHexString(buffer.AsSpan(offset, rdLength));
        }
    }

    private static string MapRcodeToString(int rcode) => rcode switch
    {
        0 => "NOERROR",
        1 => "FORMERR",
        2 => "SERVFAIL",
        3 => "NXDOMAIN",
        4 => "NOTIMP",
        5 => "REFUSED",
        _ => $"RCODE_{rcode}"
    };

    private static string MapTypeCodeToString(ushort type) => type switch
    {
        1 => "A",
        2 => "NS",
        5 => "CNAME",
        6 => "SOA",
        12 => "PTR",
        15 => "MX",
        16 => "TXT",
        28 => "AAAA",
        33 => "SRV",
        41 => "OPT",
        255 => "ANY",
        _ => $"TYPE_{type}"
    };

    private static DnsHandlerResult CreateErrorResult(string rcode, byte[] bytes) => new()
    {
        Success = false,
        ResponseCode = rcode,
        Bytes = bytes,
        Answers = Array.Empty<DnsResourceRecord>(),
        Authorities = Array.Empty<DnsResourceRecord>(),
        Additionals = Array.Empty<DnsResourceRecord>()
    };
}
