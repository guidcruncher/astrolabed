// File: src/Astrolabed.Dns/Options/ConditionalPtrRule.cs
namespace Astrolabed.Dns.Options;

/// <summary>
/// Configures a conditional forwarding rule mapping a specific IP subnet to a targeted DNS resolver for reverse (PTR) lookups.
/// </summary>
public sealed class PtrConditionalRule
{
    /// <summary>
    /// Gets or sets the IP network subnet in CIDR notation (e.g., "10.0.0.0/8" or "192.168.1.0/24") to match for reverse lookups.
    /// </summary>
    public string Subnet { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target DNS resolver IP address (e.g., "10.0.0.2") where matched PTR queries will be forwarded.
    /// </summary>
    public string TargetResolver { get; set; } = string.Empty;
}
