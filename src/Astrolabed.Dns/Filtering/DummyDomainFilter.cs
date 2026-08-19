// File: src/Astrolabed.Dns/Filtering/DummyDomainFilter.cs
using System;
using System.Collections.Generic;

using Astrolabed.Dns.Options;

using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Filtering;

public sealed class DummyDomainFilter : IDomainFilter
{
    private readonly IOptionsMonitor<DnsEngineOptions> _optionsMonitor;

    public DummyDomainFilter(IOptionsMonitor<DnsEngineOptions> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;
    }

    public bool IsAllowed(string domain)
    {
        // Explicit allowlist logic can be checked here before blocklist checking
        return false;
    }

    public bool IsBlocked(string domain, out string? reason)
    {
        reason = null;
        var options = _optionsMonitor.CurrentValue;

        var blockedSet = new HashSet<string>(options.BlockedDomains, StringComparer.OrdinalIgnoreCase);
        if (blockedSet.Contains(domain))
        {
            reason = "Blocked by security filter rule";
            return true;
        }

        return false;
    }
}
