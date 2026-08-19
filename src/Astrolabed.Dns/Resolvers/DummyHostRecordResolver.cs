// File: src/Astrolabed.Dns/Resolvers/DummyHostRecordResolver.cs
using System;
using System.Net;
using Astrolabed.Dns.Models;
using Astrolabed.Dns.Options;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Resolvers;

public sealed class DummyHostRecordResolver : IHostRecordResolver
{
    private readonly IOptionsMonitor<DnsEngineOptions> _optionsMonitor;

    public DummyHostRecordResolver(IOptionsMonitor<DnsEngineOptions> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;
    }

    public bool TryResolveHost(string domain, DnsType recordType, out IPAddress? address)
    {
        address = null;

        if (recordType != DnsType.A && recordType != DnsType.AAAA)
        {
            return false;
        }

        var options = _optionsMonitor.CurrentValue;
        if (options.Hosts.TryGetValue(domain, out var ipString) && IPAddress.TryParse(ipString, out var parsedIp))
        {
            if (recordType == DnsType.A && parsedIp.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                address = parsedIp;
                return true;
            }

            if (recordType == DnsType.AAAA && parsedIp.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                address = parsedIp;
                return true;
            }
        }

        return false;
    }
}
