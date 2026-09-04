// File: AdDomainHeuristicScanner.cs
namespace Astrolabed.Dns.Options;

/// <summary>
/// Configuration options for the heuristic ad domain scanner.
/// </summary>
public sealed class HeuristicOptions
{

    /// <summary>
    /// The configuration section key path name used when binding settings from application configuration sources.
    /// </summary>
    public const string SectionName = "HeuristicOptions";

    /// <summary>
    /// Score threshold at or above which a domain is flagged as an ad domain.
    /// </summary>
    public double ThreatThreshold { get; set; } = 50.0;

    /// <summary>
    /// List of exact domain names or suffixes explicitly whitelisted.
    /// </summary>
    public List<string> Whitelist { get; set; } = new();

    /// <summary>
    /// List of regex patterns indicating ad-serving subdomains or keywords.
    /// </summary>
    public List<string> SuspiciousKeywords { get; set; } = new()
    {
        "ad", "ads", "adservice", "telemetry", "analytics", "pixel", "tracker",
        "impression", "pagead", "doubleclick", "syndication", "banner", "sponsor"
    };
}

