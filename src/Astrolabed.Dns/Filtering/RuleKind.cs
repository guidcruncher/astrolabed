// File: src/Astrolabed.Dns/Filtering/RuleKind.cs
namespace Astrolabed.Dns.Filtering;

/// <summary>
/// Specifies the type of matching behavior for a filter rule.
/// </summary>
public enum RuleKind
{
    /// <summary>
    /// Matches exact domain strings.
    /// </summary>
    Exact,

    /// <summary>
    /// Matches exact domain and all subdomains.
    /// </summary>
    Hierarchy,

    /// <summary>
    /// Matches domains using a regular expression pattern.
    /// </summary>
    Regex
}
