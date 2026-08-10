using System.Collections.Generic;

using Astrolabed.Dns.Core;
using Astrolabed.Dns.Filtering;

using Microsoft.Extensions.Logging;

namespace Astrolabed.Dns.RuleEngine;

internal sealed partial class ResolverChainBuilder
{
    private enum DefaultChainKind
    {
        Fallback,
        DefaultResolvers,
        SingleDefault
    }

    private readonly IReadOnlyList<UpstreamEntry> _defaultChain;
    private readonly DefaultChainKind _defaultChainKind;
    private readonly ILogger _logger;

    public ResolverChainBuilder(
        DnsForwarderOptions options,
        IDnsClient defaultClient,
        IReadOnlyList<UpstreamEntry> fallback,
        IDnsClientFactory clientFactory,
        ILogger logger)
    {
        _logger = logger;

        if (fallback.Count > 0)
        {
            _defaultChain = fallback;
            _defaultChainKind = DefaultChainKind.Fallback;
        }
        else if (options.DefaultResolvers != null && options.DefaultResolvers.Count > 0)
        {
            var list = new List<UpstreamEntry>(options.DefaultResolvers.Count);
            for (int i = 0; i < options.DefaultResolvers.Count; i++)
            {
                var def = options.DefaultResolvers[i];
                var client = clientFactory.Create(def);
                list.Add(new UpstreamEntry(def.Name, client));
            }

            _defaultChain = list;
            _defaultChainKind = DefaultChainKind.DefaultResolvers;
        }
        else
        {
            _defaultChain = new[] { new UpstreamEntry("default", defaultClient) };
            _defaultChainKind = DefaultChainKind.SingleDefault;
        }
    }

    public IReadOnlyList<UpstreamEntry> BuildChain(RuleResult match, string domain, string? requestId)
    {

        if (match.Upstreams.Count > 0)
        {
            if (requestId is not null) LogUsingRuleUpstreams(_logger, requestId, domain);
            return match.Upstreams;
        }

        switch (_defaultChainKind)
        {
            case DefaultChainKind.Fallback:
                if (requestId is not null) LogUsingFallbackChain(_logger, requestId, domain);
                break;
            case DefaultChainKind.DefaultResolvers:
                if (requestId is not null) LogUsingDefaultChain(_logger, requestId, domain);
                break;
            case DefaultChainKind.SingleDefault:
                if (requestId is not null) LogUsingSingleDefault(_logger, requestId, domain);
                break;
        }

        return _defaultChain;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Request {RequestId}: Using rule-based upstreams for {Domain}")]
    private static partial void LogUsingRuleUpstreams(ILogger logger, string requestId, string domain);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Request {RequestId}: Using fallback resolver chain for {Domain}")]
    private static partial void LogUsingFallbackChain(ILogger logger, string requestId, string domain);

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "Request {RequestId}: Using default resolver chain for {Domain}")]
    private static partial void LogUsingDefaultChain(ILogger logger, string requestId, string domain);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Request {RequestId}: Using single default resolver for {Domain}")]
    private static partial void LogUsingSingleDefault(ILogger logger, string requestId, string domain);
}
