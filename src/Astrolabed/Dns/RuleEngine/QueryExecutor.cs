using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Dns.Core;
using Astrolabed.Utils;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.RuleEngine;

internal sealed class QueryExecutor
{
    private readonly IDnsCache _cache;
    private readonly DnsForwarderOptions _options;
    private readonly ILogger _logger;

    public QueryExecutor(IDnsCache cache, IOptions<DnsForwarderOptions> options, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);

        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<byte[]> ExecuteAsync(
        IReadOnlyList<UpstreamEntry> upstreams,
        DnsRequestContext context,
        CancellationToken ct)
    {
        if (upstreams.Count == 0)
        {
            return DnsResponseBuilder.BuildServfail(context.RawRequest);
        }

        int timeoutMs = _options.UpstreamTimeoutMs > 0 ? _options.UpstreamTimeoutMs : 2000;

        foreach (var upstream in upstreams)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(timeoutMs);

                var response = await upstream.Client.QueryAsync(context.RawRequest, cts.Token).ConfigureAwait(false);

                if (response != null && response.Length >= 12)
                {
                    int ttl = TtlExtractor.ExtractTtl(response);
                    if (ttl > 0 && _options.Caching?.Enabled != false)
                    {
                        _cache.Store(context, response, TimeSpan.FromSeconds(ttl));
                    }

                    return response;
                }
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning("Request {RequestId}: Timeout querying upstream {UpstreamName} for {Domain}",
                    context.RequestId, upstream.Name, context.Domain);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Request {RequestId}: Error querying upstream {UpstreamName} for {Domain}",
                    context.RequestId, upstream.Name, context.Domain);
            }
        }

        return DnsResponseBuilder.BuildServfail(context.RawRequest);
    }
}
