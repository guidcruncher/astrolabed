using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Dns.Core;
using Astrolabed.Dns;

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

        var context = new DnsRequestContext(request, requestId);

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
            if (len >= 192)
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
        if (request.Length < 12) return 512;

        ushort qdCount = BinaryPrimitives.ReadUInt16BigEndian(request.AsSpan(4, 2));
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

    private static int SkipDomainName(byte[] buffer, int offset)
    {
        while (offset < buffer.Length)
        {
            byte len = buffer[offset];
            if (len == 0) return offset + 1;
            if ((len & 0xC0) == 0xC0) return offset + 2; // Pointer
            offset += len + 1;
        }
        return -1;
    }

    private static DnsHandlerResult ParseDnsResponse(byte[] buffer)
    {
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
        }

        // Parse Authority RRs
        for (int i = 0; i < nsCount && offset < buffer.Length; i++)
        {
            if (TryParseResourceRecord(buffer, ref offset, out var record))
            {
                authorities.Add(record);
            }
        }

        // Parse Additional RRs
        for (int i = 0; i < arCount && offset < buffer.Length; i++)
        {
            if (TryParseResourceRecord(buffer, ref offset, out var record))
            {
                additionals.Add(record);
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

        string name = ReadDomainName(buffer, ref offset);
        if (offset + 10 > buffer.Length) return false;

        ushort typeCode = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset, 2));
        ushort classCode = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset + 2, 2));
        uint ttl = BinaryPrimitives.ReadUInt32BigEndian(buffer.AsSpan(offset + 4, 4));
        ushort rdLength = BinaryPrimitives.ReadUInt16BigEndian(buffer.AsSpan(offset + 8, 2));

        offset += 10;

        if (offset + rdLength > buffer.Length) return false;

        string data = ParseRecordData(buffer, offset, typeCode, rdLength);
        offset += rdLength;

        record = new DnsResourceRecord
        {
            Name = name,
            Type = MapTypeCodeToString(typeCode),
            Class = classCode == 1 ? "IN" : classCode.ToString(),
            TimeToLive = ttl,
            Data = data
        };

        return true;
    }

    private static string ReadDomainName(byte[] buffer, ref int offset)
    {
        var sb = new StringBuilder();
        int current = offset;
        bool jumped = false;
        int originalOffset = offset;

        while (current < buffer.Length && buffer[current] != 0)
        {
            if ((buffer[current] & 0xC0) == 0xC0)
            {
                if (!jumped)
                {
                    originalOffset = current + 2;
                    jumped = true;
                }
                current = ((buffer[current] & 0x3F) << 8) | buffer[current + 1];
                continue;
            }

            int len = buffer[current++];
            if (sb.Length > 0) sb.Append('.');
            sb.Append(Encoding.ASCII.GetString(buffer, current, len));
            current += len;
        }

        offset = jumped ? originalOffset : current + 1;
        return sb.ToString();
    }

    private static string ParseRecordData(byte[] buffer, int offset, ushort typeCode, ushort rdLength)
    {
        return typeCode switch
        {
            1 when rdLength == 4 => new IPAddress(buffer[offset..(offset + 4)]).ToString(),
            28 when rdLength == 16 => new IPAddress(buffer[offset..(offset + 16)]).ToString(),
            2 or 5 or 12 => ReadDomainName(buffer, ref offset),
            16 => Encoding.UTF8.GetString(buffer, offset, rdLength),
            _ => BitConverter.ToString(buffer, offset, rdLength).Replace("-", string.Empty)
        };
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
