using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Events;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Core;

public sealed class DnsForwarderService
{
    private readonly ILogger<DnsForwarderService> _logger;
    private readonly DnsForwarderOptions _options;
    private readonly RuleEngine.RuleEngine _ruleEngine;

    public DnsForwarderService(
        ILogger<DnsForwarderService> logger,
        IOptions<DnsForwarderOptions> options,
        RuleEngine.RuleEngine ruleEngine)
    {
        _logger = logger;
        _options = options.Value;
        _ruleEngine = ruleEngine;
    }

    public async Task<PooledBuffer?> ProcessAsync(
        byte[] request,
        IPEndPoint remote,
        CancellationToken ct)
    {
        if (request == null || request.Length < 12)
        {
            return null;
        }

        string requestId = _logger.IsEnabled(LogLevel.Debug)
            ? Guid.CreateVersion7().ToString("N")
            : string.Empty;

        var context = new DnsRequestContext(request, requestId, remote.Address.ToString());

        if (string.IsNullOrEmpty(context.Domain))
        {
            if (!string.IsNullOrEmpty(requestId))
            {
                _logger.LogWarning("Request {RequestId}: Received DNS message with no questions from {Remote}", requestId, remote);
            }
            return null;
        }

        var response = await _ruleEngine.QueryAsync(context, ct);

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
}

