using System.Buffers;
using System.Net;

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
        // Generate correlation ID only if debug logging is active to save string allocations
        string? requestId = _logger.IsEnabled(LogLevel.Debug)
            ? Guid.CreateVersion7().ToString("N")
            : null;

        if (requestId is not null)
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
            if (requestId is not null)
            {
                _logger.LogWarning(
                    "Request {RequestId}: Received DNS message with no questions from {Remote}",
                    requestId,
                    remote);
            }
            return null;
        }

        var ruleResult = _ruleEngine.Match(q.Name, requestId);

        // --- FAST PATH 1: BLOCK RULE ---
        if (ruleResult.Block)
        {
            var blocked = DnsParser.BuildBlockedResponse(message);
            blocked[0] = request[0];
            blocked[1] = request[1];

            return new PooledBuffer(blocked, blocked.Length, fromPool: false);
        }

        // --- FAST PATH 2: CACHE CHECK ---
        if (_ruleEngine.Cache.TryGetPooled(q.Name, out var cachedBuf, out var cachedLen))
        {
            _metrics.RecordDnsCacheHit();

            var sendBuf = ArrayPool<byte>.Shared.Rent(cachedLen);
            Buffer.BlockCopy(cachedBuf!, 0, sendBuf, 0, cachedLen);

            sendBuf[0] = request[0];
            sendBuf[1] = request[1];

            return new PooledBuffer(sendBuf, cachedLen, fromPool: true);
        }

        // Validate upstream collection safety
        if (ruleResult.Upstreams.Count == 0)
        {
            return null;
        }

        // --- SLOW PATH: NETWORK FORWARDING ---
        var response = await _ruleEngine.QueryAsync(q.Name, request, requestId, ruleResult, ct);

        response[0] = request[0];
        response[1] = request[1];

        return new PooledBuffer(response, response.Length, fromPool: false);
    }
}
