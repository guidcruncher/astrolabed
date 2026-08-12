using System;
using System.Collections.Generic;
using System.Net;

using Astrolabed.Dns.Core;
using Astrolabed.Dns.Filtering;
using Astrolabed.Utils;

using Microsoft.Extensions.Logging;

namespace Astrolabed.Dns.RuleEngine;

internal sealed class RuleMatcher
{
    private readonly RuleCompiler _compiler;
    private readonly ILogger _logger;

    public RuleMatcher(RuleCompiler compiler, ILogger logger)
    {
        _compiler = compiler;
        _logger = logger;
    }

    private static RuleResult HostOverride(IPAddress ip) =>
        new(new List<UpstreamEntry>(1) { new("hosts", new StaticDnsClient(ip)) }, false);

    public RuleResult Match(string domain, string? requestId)
    {
        bool isDebug = _logger.IsEnabled(LogLevel.Debug);
        string lower = ToLowerFast(domain);

        var hostIp = _compiler.Hosts.MatchMostSpecific(lower);
        if (hostIp != null)
        {
            if (isDebug)
            {
                _logger.LogDebug("Request {RequestId}: Hosts override matched for {Domain}", requestId, domain);
            }
            return HostOverride(hostIp);
        }

        var allow = ListPool<UpstreamEntry>.Rent();
        UpstreamEntry? block = null;

        try
        {
            if (_compiler.Exact.TryGetValue(lower, out var ex))
            {
                Apply(ex, allow, ref block);
            }

            foreach (var r in _compiler.Suffix.MatchAll(lower))
            {
                Apply(r, allow, ref block);
            }

            foreach (var r in _compiler.Prefix.MatchAll(lower))
            {
                Apply(r, allow, ref block);
            }

            foreach (var r in _compiler.Aho.Match(lower))
            {
                Apply(r, allow, ref block);
            }

            foreach (var r in _compiler.RegexRules)
            {
                if (r.Regex != null && r.Regex.IsMatch(domain))
                {
                    Apply(r, allow, ref block);
                }
            }

            if (allow.Count > 0)
            {
                if (isDebug)
                {
                    _logger.LogDebug("Request {RequestId}: Allow rules matched for {Domain}", requestId, domain);
                }
                return new RuleResult(new List<UpstreamEntry>(allow), false);
            }

            if (block != null)
            {
                if (isDebug)
                {
                    _logger.LogDebug("Request {RequestId}: Blocking domain {Domain} due to rule {Rule}",
                        requestId, domain, block.Value.Name);
                }
                return new RuleResult(new List<UpstreamEntry>(1) { block.Value }, true);
            }
        }
        finally
        {
            ListPool<UpstreamEntry>.Return(allow);
        }

        if (_compiler.FallbackResolvers.Count == 0)
        {
            if (isDebug)
            {
                _logger.LogDebug("Request {RequestId}: No rules matched; using primary default resolver for {Domain}",
                    requestId, domain);
            }

            return new RuleResult(
                new List<UpstreamEntry>(1)
                {
                    new("default", _compiler.DefaultClient)
                },
                false);
        }

        return new RuleResult(new List<UpstreamEntry>(0), false);
    }

    private void Apply(CompiledRule r, List<UpstreamEntry> allow, ref UpstreamEntry? block)
    {
        if (!r.Block)
        {
            allow.Add(new UpstreamEntry(r.Name, r.Client ?? _compiler.DefaultClient));
        }
        else
        {
            block ??= new UpstreamEntry(r.Name, r.Client ?? _compiler.DefaultClient);
        }
    }

    private static string ToLowerFast(string s)
    {
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c >= 'A' && c <= 'Z')
            {
                return s.ToLowerInvariant();
            }
        }
        return s;
    }
}
