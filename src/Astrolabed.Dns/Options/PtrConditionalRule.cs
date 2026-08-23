// File: src/Astrolabed.Dns/Options/ConditionalPtrRule.cs
namespace Astrolabed.Dns.Options;

public sealed class PtrConditionalRule
{
    public string Subnet { get; set; } = string.Empty; // e.g., "10.0.0.0/8" or "192.168.1.0/24"
    public string TargetResolver { get; set; } = string.Empty; // e.g., "10.0.0.2"
}
