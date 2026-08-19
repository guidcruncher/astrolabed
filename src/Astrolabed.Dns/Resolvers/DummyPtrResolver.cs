// File: src/Astrolabed.Dns/Resolvers/DummyPtrResolver.cs
using Astrolabed.Dns.Options;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Resolvers;

public sealed class DummyPtrResolver : IPtrResolver
{
    private readonly IOptionsMonitor<DnsEngineOptions> _optionsMonitor;

    public DummyPtrResolver(IOptionsMonitor<DnsEngineOptions> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;
    }

    public bool TryResolvePtr(string ptrQuery, out string? domainName)
    {
        domainName = null;
        var options = _optionsMonitor.CurrentValue;

        if (options.PtrRecords.TryGetValue(ptrQuery, out var matchedDomain))
        {
            domainName = matchedDomain;
            return true;
        }

        return false;
    }
}
