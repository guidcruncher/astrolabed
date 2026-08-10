using System;
using System.Buffers;
using System.Linq;
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

        // RuleEngine.QueryAsync executes cache lookup, block rule matching, and upstream querying internally
        var response = await _ruleEngine.QueryAsync(domain, request, requestId, ct);

        if (response is null || response.Length == 0)
        {
            return null;
        }

        return new PooledBuffer(response, response.Length, fromPool: false);
    }
}
