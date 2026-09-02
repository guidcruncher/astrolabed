// File: src/Astrolabed.Dns/Filtering/FilterRule.cs
using System.Net;
using System.Text.RegularExpressions;

namespace Astrolabed.Dns.Filtering;

/// <summary>
/// Represents a compiled domain filter rule item.
/// </summary>
/// <param name="Pattern">The domain or regex pattern string.</param>
/// <param name="RuleKind">The matching kind of rule.</param>
/// <param name="IsAllow">Indicates whether this rule is an allowlist exception.</param>
/// <param name="ListId">The source list identifier.</param>
/// <param name="IpAddress">The associated IP address if parsed from a hosts list entry.</param>
/// <param name="CompiledRegex">The precompiled regular expression object if rule kind is regex.</param>
public sealed record FilterRule(
    string Pattern,
    RuleKind RuleKind,
    bool IsAllow,
    int ListId,
    IPAddress? IpAddress = null,
    Regex? CompiledRegex = null)
{
    /// <summary>
    /// Gets a value indicating whether this rule specifies an IP address override.
    /// </summary>
    public bool HasIpAddress => IpAddress is not null;
}
