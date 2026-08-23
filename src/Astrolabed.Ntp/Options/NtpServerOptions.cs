using Astrolabed.Core.Options;

namespace Astrolabed.Ntp.Options;

/// <summary>
/// Configures settings for the NTP server service, including network listening bindings, server characteristics, and upstream synchronization options.
/// </summary>
public class NtpServerOptions
{
    /// <summary>
    /// The configuration section key path name used when binding settings from application configuration sources.
    /// </summary>
    public const string SectionName = "NtpServer";

    /// <summary>
    /// Gets or sets the network binding and address options configured for listening for incoming NTP requests.
    /// </summary>
    public AddressOptions ListenAddress { get; set; } = new();

    /// <summary>
    /// Gets or sets the stratum level advertised by the server, ranging from 1 (primary reference clock) to 15 (secondary server).
    /// </summary>
    public byte Stratum { get; set; } = 1;

    /// <summary>
    /// Gets or sets the 4-character ASCII string or IP identifier representing the clock reference source (e.g., "LOCL", "GPS", "ATOM").
    /// </summary>
    public string ReferenceIdentifier { get; set; } = "LOCL";

    /// <summary>
    /// Gets or sets the exponent representing system clock precision in log2 seconds (e.g., -20 corresponds to approximately 1 microsecond).
    /// </summary>
    public sbyte Precision { get; set; } = -20;

    /// <summary>
    /// Gets or sets the minimum polling interval exponent in log2 seconds between outgoing requests.
    /// </summary>
    public byte PollInterval { get; set; } = 4;

    /// <summary>
    /// Gets or sets the total round-trip delay to the primary reference source in NTP short format unit.
    /// </summary>
    public uint RootDelay { get; set; } = 0;

    /// <summary>
    /// Gets or sets the nominal error bound relative to the primary reference source in NTP short format unit.
    /// </summary>
    public uint RootDispersion { get; set; } = 0;

    /// <summary>
    /// Gets or sets the time provider resolution mode utilized to answer incoming client queries.
    /// </summary>
    public TimeResolverMode ResolverMode { get; set; } = TimeResolverMode.Local;

    /// <summary>
    /// Gets or sets the list of upstream NTP server hostname or IP endpoint strings used when resolving network time.
    /// </summary>
    public List<string> UpstreamServers { get; set; } = new()
    {
        "pool.ntp.org",
        "time.nist.gov",
        "time.google.com"
    };

    /// <summary>
    /// Gets or sets the UDP destination port used when polling upstream NTP servers. Defaults to 123.
    /// </summary>
    public int UpstreamPort { get; set; } = 123;

    /// <summary>
    /// Gets or sets the maximum time in milliseconds to await a response from an upstream server before timing out.
    /// </summary>
    public int UpstreamTimeoutMilliseconds { get; set; } = 3000;

    /// <summary>
    /// Gets or sets the interval in seconds between scheduled upstream synchronization operations.
    /// </summary>
    public int UpstreamSyncIntervalSeconds { get; set; } = 60;
}
