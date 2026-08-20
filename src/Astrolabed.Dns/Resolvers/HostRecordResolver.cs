// File: src/Astrolabed.Dns/Resolvers/HostRecordResolver.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

using Astrolabed.Dns.Models;

namespace Astrolabed.Dns.Resolvers;

public class HostRecordResolver : IHostRecordResolver
{
    private readonly IReadOnlyList<HostsEntry> _hostsEntries;

    public HostRecordResolver(IReadOnlyList<HostsEntry> hostsEntries)
    {
        _hostsEntries = hostsEntries;
    }

    public bool TryResolveHost(string domain, DnsType recordType, out IPAddress? address)
    {
        address = null;

        if (string.IsNullOrWhiteSpace(domain))
        {
            return false;
        }

        // Normalize string: Trim whitespace and trailing DNS root dot '.'
        string normalizedDomain = domain.Trim().TrimEnd('.');

        // Perform case-insensitive match against loaded entries
        var match = _hostsEntries.FirstOrDefault(e =>
            string.Equals(e.Hostname.TrimEnd('.'), normalizedDomain, StringComparison.OrdinalIgnoreCase));

        if (match == null || match.Addresses == null || match.Addresses.Count == 0)
        {
            return false;
        }

        // Match IP family against requested query record type
        address = recordType switch
        {
            DnsType.A => match.Addresses.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork),
            DnsType.AAAA => match.Addresses.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetworkV6),
            _ => null
        };

        return address != null;
    }
}
