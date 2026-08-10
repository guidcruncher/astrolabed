using System.Net;
using System.Text.RegularExpressions;

using Astrolabed.Dns.Core;
using Astrolabed.Dns.Filtering;
using Astrolabed.Utils;

using Microsoft.Extensions.Logging;

namespace Astrolabed.Dns.RuleEngine;

internal sealed class ResolverChainBuilder
{
    private readonly DnsForwarderOptions _options;
    private readonly IDnsClient _defaultClient;
    private readonly IReadOnlyList<UpstreamEntry> _fallback;
    private readonly ILogger _logger;
    private readonly IDnsClientFactory _clientFactory;

    public ResolverChainBuilder(
        DnsForwarderOptions options,
        IDnsClient defaultClient,
        IReadOnlyList<UpstreamEntry> fallback,
        IDnsClientFactory clientFactory,
        ILogger logger)
    {
        _options = options;
        _defaultClient = defaultClient;
        _fallback = fallback;
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public List<UpstreamEntry> BuildChain(RuleResult match, string domain, string requestId)
    {
        if (match.Upstreams.Count > 0)
        {
            _logger.LogDebug("Request {RequestId}: Using rule-based upstreams for {Domain}", requestId, domain);
            return new List<UpstreamEntry>(match.Upstreams);
        }

        if (_fallback.Count > 0)
        {
            _logger.LogDebug("Request {RequestId}: Using fallback resolver chain for {Domain}", requestId, domain);
            return new List<UpstreamEntry>(_fallback);
        }

        var upstreams = new List<UpstreamEntry>();

        if (_options.DefaultResolvers != null && _options.DefaultResolvers.Count > 0)
        {
            _logger.LogDebug("Request {RequestId}: Using default resolver chain for {Domain}", requestId, domain);

            foreach (var def in _options.DefaultResolvers)
            {
                var client = _clientFactory.Create(def);
                upstreams.Add(new UpstreamEntry(def.Name, client));
            }

            return upstreams;
        }

        _logger.LogDebug("Request {RequestId}: Using single default resolver for {Domain}", requestId, domain);
        upstreams.Add(new UpstreamEntry("default", _defaultClient));
        return upstreams;
    }
}
