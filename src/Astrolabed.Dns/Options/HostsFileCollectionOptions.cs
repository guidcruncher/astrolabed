// File: src/Astrolabed.Dns/Options/HostsFileCollectionOptions.cs
namespace Astrolabed.Dns.Options;

public sealed class HostsFileCollectionOptions
{
    public const string SectionName = "HostsFiles";

    public List<string> Sources { get; set; } = new();
}
