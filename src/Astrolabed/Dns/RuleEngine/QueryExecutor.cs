using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    private readonly ConcurrentDictionary<string, CircuitState> _circuits = new();
    private readonly TimeSpan _perUpstreamTimeout;

    private static readonly long CircuitBreakerDurationMs = 30_000; // 30 seconds

    public QueryExecutor(DnsCache cache, DnsForwarderOptions options, ILogger logger)
    {
        _cache = cache;
        _logger = logger;
        int timeoutMs = options.UpstreamTimeoutMs > 0 ? options.UpstreamTimeoutMs : 1500;
        _perUpstreamTimeout = TimeSpan.FromMilliseconds(timeoutMs);
    }

    public async Task<byte[]?> ExecuteAsync(
        IReadOnlyList<UpstreamEntry> upstreams,
        DnsRequestContext context,
        CancellationToken ct)
    {
        int count = upstreams.Count;
        if (count == 0) return null;

        for (int i = 0; i < count; i++)
        {
            var upstream = upstreams[i];
            var circuit = _circuits.GetOrAdd(upstream.Name, _ => new CircuitState());

            if (circuit.IsUnhealthy())
            {
                LogCircuitOpen(_logger, context.RequestId, upstream.Name);
                continue;
            }

            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(_perUpstreamTimeout);

                if (!string.IsNullOrEmpty(context.RequestId))
                {
                    LogQueryingUpstream(_logger, context.RequestId, upstream.Name, context.Domain);
                }

                var resp = await upstream.Client.QueryAsync(context.RawRequest, timeoutCts.Token).ConfigureAwait(false);

                if (resp == null || resp.Length < 4)
                {
                    circuit.RecordFailure();
                    continue;
                }

                int rcode = resp[3] & 0x0F;
                if (rcode == 2) // SERVFAIL
                {
                    if (!string.IsNullOrEmpty(context.RequestId))
                    {
                        LogServfail(_logger, context.RequestId, upstream.Name, context.Domain);
                    }
                    circuit.RecordFailure();
                    continue;
                }

                circuit.RecordSuccess();

                int ttl = TtlExtractor.ExtractTtl(resp);
                if (ttl > 0)
                {
                    if (!string.IsNullOrEmpty(context.RequestId))
                    {
                        LogTtlFound(_logger, context.RequestId, context.Domain, ttl, upstream.Name);
                    }
                    _cache.Store(context, resp, TimeSpan.FromSeconds(ttl));
                }
                else if (!string.IsNullOrEmpty(context.RequestId))
                {
                    LogNoTtlFound(_logger, context.RequestId, context.Domain, upstream.Name);
                }

                return resp;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                circuit.RecordFailure();
                if (!string.IsNullOrEmpty(context.RequestId))
                {
                    LogErrorQueryingUpstream(_logger, ex, context.RequestId, upstream.Name, context.Domain);
                }
            }
        }

        return null;
    }

    private sealed class CircuitState
    {
        private int _consecutiveFailures;
        private long _openUntilTick;

        public bool IsUnhealthy()
        {
            return Environment.TickCount64 < Volatile.Read(ref _openUntilTick);
        }

        public void RecordSuccess()
        {
            Interlocked.Exchange(ref _consecutiveFailures, 0);
        }

        public void RecordFailure()
        {
            if (Interlocked.Increment(ref _consecutiveFailures) >= 3)
            {
                Volatile.Write(ref _openUntilTick, Environment.TickCount64 + CircuitBreakerDurationMs);
            }
        }
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

    [LoggerMessage(EventId = 6, Level = LogLevel.Warning, Message = "Request {RequestId}: Skipping unhealthy upstream {Upstream} (Circuit Open)")]
    private static partial void LogCircuitOpen(ILogger logger, string requestId, string upstream);
}

