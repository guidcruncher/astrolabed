// File: src/Astrolabed.Dns/Options/HostsFileCollectionOptions.cs
namespace Astrolabed.Dns.Options;

/// <summary>
/// Configures settings for loading external hosts files into the local DNS engine.
/// </summary>
public sealed class HostsFileCollectionOptions
{
    /// <summary>
    /// The configuration section key path name used when binding settings from application configuration sources.
    /// </summary>
    public const string SectionName = "HostsFiles";

    /// <summary>
    /// Gets or sets the list of file paths or remote URLs containing hosts file definitions.
    /// </summary>
    public List<string> Sources { get; set; } = new();
}
