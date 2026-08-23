// File: src/Astrolabed.Dns/Options/DomainFilterRuleOptions.cs
namespace Astrolabed.Dns.Options;

/// <summary>
/// Configures settings and file or URL source paths for domain allowlist and blocklist filtering rules.
/// </summary>
public sealed class DomainFilterRuleOptions
{
    /// <summary>
    /// The configuration section key path name used when binding settings from application configuration sources.
    /// </summary>
    public const string SectionName = "DomainFilterRules";

    /// <summary>
    /// Gets or sets the list of file paths or remote URLs specifying domain allowlist rules.
    /// </summary>
    public List<string> AllowListSources { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of file paths or remote URLs specifying domain blocklist rules.
    /// </summary>
    public List<string> BlockListSources { get; set; } = [];
}
