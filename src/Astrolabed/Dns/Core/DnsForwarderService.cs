using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Dns.Core;
using Astrolabed.Events;

using Microsoft.Extensions.Logging;

namespace Astrolabed.Dns.Core;

public sealed class DnsForwarderService
{
    private readonly ILogger<DnsForwarderService> _logger;
    private readonly DnsForwarderOptions _options;
    private readonly IDnsClient _defaultClient;
    private readonly RuleEngine.RuleEngine _ruleEngine;
    private readonly IDnsMetrics _metrics;

    public DnsForwarderService(
        ILogger<DnsForwarderService> logger,
        DnsForwarderOptions options,
        IDnsClient defaultClient,
        RuleEngine.RuleEngine ruleEngine,
        IDnsMetrics metrics)
    {
        _logger = logger;
        _options = options;
        _defaultClient = defaultClient;
        _ruleEngine = ruleEngine;
        _metrics = metrics;
    }

    public async Task<PooledBuffer?> ProcessAsync(
        byte[] request,
        IPEndPoint remote,
        CancellationToken ct)
    {
        string requestId = _logger.IsEnabled(LogLevel.Debug)
            ? Guid.CreateVersion7().ToString("N")
            : string.Empty;

        if (!string.IsNullOrEmpty(requestId))
        {
            _logger.LogDebug(
                "Request {RequestId}: Received DNS request from {Remote} ({Length} bytes)",
                requestId,
                remote,
                request.Length);
        }

        var message = DnsParser.Parse(request);
        var q = message.Questions.FirstOrDefault();

        if (q is null)
        {
            if (!string.IsNullOrEmpty(requestId))
            {
                _logger.LogWarning(
                    "Request {RequestId}: Received DNS message with no questions from {Remote}",
                    requestId,
                    remote);
            }
            return null;
        }

        string domain = q.Name ?? string.Empty;

        var response = await _ruleEngine.QueryAsync(domain, request, requestId, ct);

        if (response is null || response.Length == 0)
        {
            return null;
        }

        // Apply TCP Truncation fallback if payload exceeds UDP boundaries
        response = ApplyTruncationIfNeeded(response, request);

        return new PooledBuffer(response, response.Length, fromPool: false);
    }

    private static byte[] ApplyTruncationIfNeeded(byte[] response, byte[] request)
    {
        // Check for EDNS0 (Additional Records Count > 0)
        int arCount = request.Length >= 12 ? BinaryPrimitives.ReadUInt16BigEndian(request.AsSpan(10, 2)) : 0;

        // 512 is standard UDP limit. 1232 is the modern DNS Flag Day safe maximum for EDNS0.
        int maxPayloadSize = arCount > 0 ? 1232 : 512;

        if (response.Length <= maxPayloadSize)
        {
            return response;
        }

        // Truncate response: 12 bytes header + Question section
        int offset = 12;
        while (offset < response.Length)
        {
            byte len = response[offset];
            if (len == 0) { offset += 5; break; }
            if (len >= 192) { offset += 6; break; }
            offset += len + 1;
        }

        if (offset > response.Length) offset = 12; // Fallback for malformed response

        var truncated = new byte[offset];
        Buffer.BlockCopy(response, 0, truncated, 0, offset);

        // Set TC (Truncated) bit - Byte 2, Bit 1 (0x02)
        truncated[2] |= 0x02;

        // Clear ANCOUNT, NSCOUNT, ARCOUNT to 0 (Bytes 6 through 11)
        truncated.AsSpan(6, 6).Clear();

        return truncated;
    }
}
