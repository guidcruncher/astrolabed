using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Dns.Core;
using Astrolabed.Dns.Filtering;
using Astrolabed.Utils;

using Microsoft.Extensions.Logging;

namespace Astrolabed.Dns.RuleEngine;

internal sealed partial class QueryExecutor
{
    private readonly DnsCache _cache;
    private readonly ILogger _logger;

    public QueryExecutor(DnsCache cache, ILogger logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<byte[]?> ExecuteAsync(
        IReadOnlyList<UpstreamEntry> upstreams,
        string domain,
        byte[] request,
        string? requestId,
        CancellationToken ct)
    {
        ushort qType = (ushort)DnsType.A;
        var parsedReq = DnsMessage.TryParse(request);
        if (parsedReq?.Questions.Count > 0)
        {
            qType = (ushort)parsedReq.Questions[0].Type;
        }

        int count = upstreams.Count;
        for (int i = 0; i < count; i++)
        {
            var upstream = upstreams[i];
            try
            {
                if (requestId is not null)
                    LogQueryingUpstream(_logger, requestId, upstream.Name, domain);

                var resp = await upstream.Client.QueryAsync(request, ct).ConfigureAwait(false);

                if (resp.Length < 4)
                {
                    continue;
                }

                int rcode = resp[3] & 0x0F;

                if (rcode == 2)
                {
                    if (requestId is not null)
                        LogServfail(_logger, requestId, upstream.Name, domain);
                    continue;
                }

                int ttl = TtlExtractor.ExtractTtl(resp);

                if (ttl > 0)
                {
                    if (requestId is not null)
                        LogTtlFound(_logger, requestId, domain, ttl, upstream.Name);
                    _cache.Store(domain, qType, resp, TimeSpan.FromSeconds(ttl));
                }
                else
                {
                    if (requestId is not null)
                        LogNoTtlFound(_logger, requestId, domain, upstream.Name);
                }

                return resp;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (requestId is not null)
                    LogErrorQueryingUpstream(_logger, ex, requestId, upstream.Name, domain);
            }
        }

        return null;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Request {RequestId}: Querying upstream {Upstream} for {Domain}")]
    private static partial void LogQueryingUpstream(ILogger logger, string requestId, string upstream, string domain);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Request {RequestId}: Upstream {Upstream} returned SERVFAIL for {Domain}")]
    private static partial void LogServfail(ILogger logger, string requestId, string upstream, string domain);

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "Request {RequestId}: TTL for {Domain} is {Ttl}s (via {Upstream})")]
    private static partial void LogTtlFound(ILogger logger, string requestId, string domain, int ttl, string upstream);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "Request {RequestId}: No TTL found for {Domain} (via {Upstream})")]
    private static partial void LogNoTtlFound(ILogger logger, string requestId, string domain, string upstream);

    [LoggerMessage(EventId = 5, Level = LogLevel.Error, Message = "Request {RequestId}: Error querying upstream {Upstream} for {Domain}")]
    private static partial void LogErrorQueryingUpstream(ILogger logger, Exception ex, string requestId, string upstream, string domain);
}
