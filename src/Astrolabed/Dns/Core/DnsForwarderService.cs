using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Events;

using Microsoft.Extensions.Logging;

namespace Astrolabed.Dns.Core;

public sealed class DnsForwarderService
{
    private readonly ILogger<DnsForwarderService> _logger;
    private readonly DnsForwarderOptions _options;
    private readonly RuleEngine.RuleEngine _ruleEngine;

    public DnsForwarderService(
        ILogger<DnsForwarderService> logger,
        DnsForwarderOptions options,
        RuleEngine.RuleEngine ruleEngine)
    {
        _logger = logger;
        _options = options;
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

        // Single Parse Entry Point: Converts wire bytes to context once
        var context = new DnsRequestContext(request, requestId);

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

        response = ApplyTruncationIfNeeded(response, request);

        return new PooledBuffer(response, response.Length, fromPool: false);
    }

    private static byte[] ApplyTruncationIfNeeded(byte[] response, byte[] request)
    {
        int arCount = request.Length >= 12 ? BinaryPrimitives.ReadUInt16BigEndian(request.AsSpan(10, 2)) : 0;
        int maxPayloadSize = arCount > 0 ? 1232 : 512;

        if (response.Length <= maxPayloadSize)
        {
            return response;
        }

        int offset = 12;
        while (offset < response.Length)
        {
            byte len = response[offset];
            if (len == 0) { offset += 5; break; }
            if (len >= 192) { offset += 6; break; }
            offset += len + 1;
        }

        if (offset > response.Length) offset = 12;

        var truncated = new byte[offset];
        Buffer.BlockCopy(response, 0, truncated, 0, offset);

        truncated[2] |= 0x02; // Set TC bit
        truncated.AsSpan(6, 6).Clear(); // Clear section counts

        return truncated;
    }
}
