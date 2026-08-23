using System.Net;

using Astrolabed.Dns.Models;

namespace Astrolabed.Dns.Resolvers;

/// <summary>
/// Defines host resolution operations for querying local hosts file entries.
/// </summary>
public interface IHostRecordResolver
{
    /// <summary>
    /// Attempts to resolve a hostname to an IP address matching the requested DNS query record type.
    /// </summary>
    /// <param name="domain">The domain name to resolve.</param>
    /// <param name="recordType">The DNS query record type (e.g., A or AAAA).</param>
    /// <param name="address">Outputs the matching IP address if found; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if a matching host entry was found; otherwise, <c>false</c>.</returns>
    bool TryResolveHost(string domain, DnsType recordType, out IPAddress? address);

    /// <summary>
    /// Attempts to resolve a hostname span to an IP address matching the requested DNS query record type.
    /// </summary>
    /// <param name="domain">The domain name span to resolve.</param>
    /// <param name="recordType">The DNS query record type (e.g., A or AAAA).</param>
    /// <param name="address">Outputs the matching IP address if found; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if a matching host entry was found; otherwise, <c>false</c>.</returns>
    bool TryResolveHost(ReadOnlySpan<char> domain, DnsType recordType, out IPAddress? address) =>
        TryResolveHost(domain.ToString(), recordType, out address);
}

