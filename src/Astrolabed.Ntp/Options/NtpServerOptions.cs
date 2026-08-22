using Astrolabed.Core.Options;

namespace Astrolabed.Ntp.Options;

public class NtpServerOptions
{
    public const string SectionName = "NtpServer";

    public AddressOptions ListenAddress { get; set; } = new();
    public byte Stratum { get; set; } = 1;
    public string ReferenceIdentifier { get; set; } = "LOCL";
    public sbyte Precision { get; set; } = -20;
    public byte PollInterval { get; set; } = 4;
    public uint RootDelay { get; set; } = 0;
    public uint RootDispersion { get; set; } = 0;

    public TimeResolverMode ResolverMode { get; set; } = TimeResolverMode.Local;
    public List<string> UpstreamServers { get; set; } = new()
    {
        "pool.ntp.org",
        "time.nist.gov",
        "time.google.com"
    };
    public int UpstreamPort { get; set; } = 123;
    public int UpstreamTimeoutMilliseconds { get; set; } = 3000;
    public int UpstreamSyncIntervalSeconds { get; set; } = 60;
}
