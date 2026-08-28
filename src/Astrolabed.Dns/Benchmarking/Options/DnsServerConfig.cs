// File: DnsServerConfig.cs
namespace Astrolabed.Dns.Benchmarking.Options;

using System.Collections.Generic;

/// <summary>
/// Represents the configuration and endpoints for a specific public DNS provider.
/// </summary>
public sealed class DnsServerConfig
{
    /// <summary>
    /// Gets or sets the friendly name of the DNS provider (e.g., "Cloudflare", "Google").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the informational homepage URL for the provider.
    /// </summary>
    public string Homepage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of IPv4 addresses associated with this provider.
    /// </summary>
    public List<string> Ipv4 { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of IPv6 addresses associated with this provider.
    /// </summary>
    public List<string> Ipv6 { get; set; } = [];
}
