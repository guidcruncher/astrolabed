// File: src/Astrolabed.Dns/Services/NetworkScannerOptions.cs
using System.ComponentModel.DataAnnotations;

namespace Astrolabed.Dns.Services;

/// <summary>
/// Configuration options for controlling local area network (LAN) scanning parameters.
/// </summary>
public sealed class NetworkScannerOptions
{
    /// <summary>
    /// The configuration section key in appsettings.json or configuration providers.
    /// </summary>
    public const string SectionName = "NetworkScanner";

    /// <summary>
    /// Gets or sets the maximum number of concurrent ICMP ping requests permitted during network discovery.
    /// </summary>
    [Range(1, 1000)]
    public int MaxDegreeOfParallelism { get; init; } = 100;

    /// <summary>
    /// Gets or sets the ICMP ping timeout in milliseconds for each probed host address.
    /// </summary>
    [Range(10, 30000)]
    public int PingTimeoutMs { get; init; } = 200;
}
