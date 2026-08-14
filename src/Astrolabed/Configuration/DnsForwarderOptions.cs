namespace Astrolabed.Dns;

public sealed class DnsForwarderOptions
{

    public const string SectionName = "Dns";

    public ListenOptions Listen { get; set; } = new();
    public List<UpstreamResolverOptions> DefaultResolvers { get; set; } = new();
    public List<UpstreamResolverOptions> Resolvers { get; set; } = new();
    public CachingOptions Caching { get; set; } = new();

    public List<string> Allowlists { get; set; } = new();
    public List<string> Blocklists { get; set; } = new();

    public List<string> HostsFiles { get; set; } = new();

    public BlockResponseOptions BlockResponse { get; set; } = new();

    public int UpstreamTimeoutMs { get; set; } = 1500;

    public ConditionalForwarderOptions ConditionalForwarding { get; set; } = new();

}

public sealed class ListenOptions
{
    public const string SectionName = "Dns:Listen";

    public string Address { get; set; } = "0.0.0.0";
    public int Port { get; set; } = 53;
}

public sealed class UpstreamResolverOptions
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = "1.1.1.1";
    public int Port { get; set; } = 53;
    public string? Rule { get; set; }
    public bool Block { get; set; }
}

public sealed class CachingOptions
{
    public const string SectionName = "Dns:Caching";

    public bool Enabled { get; set; } = true;
    public int TtlSeconds { get; set; } = 300;
    public int MaxEntries { get; set; } = 10000;
    public int CleanupIntervalMinutes { get; set; } = 5;
}
