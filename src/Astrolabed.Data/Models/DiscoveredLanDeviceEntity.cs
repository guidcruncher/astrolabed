using System.Net;

namespace Astrolabed.Data.Models;

/// <summary>
/// Database entity representation for a discovered LAN device.
/// Maps directly to the underlying SQL table structure where IP addresses are stored as strings
/// and timestamps are stored as Unix epoch seconds (bigint/long).
/// </summary>
public sealed class DiscoveredLanDeviceEntity
{
    public required string IpAddress { get; set; }
    public required string MacAddress { get; set; }
    public string? HostName { get; set; }
    public required long LastSeen { get; set; }
    public required long FirstSeen { get; set; }

    /// <summary>
    /// Creates a persistence entity instance from a domain record.
    /// </summary>
    public static DiscoveredLanDeviceEntity FromDomain(DiscoveredLanDevice domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        return new DiscoveredLanDeviceEntity
        {
            IpAddress = domain.IpAddress.ToString(),
            MacAddress = domain.MacAddress,
            HostName = domain.HostName,
            LastSeen = domain.LastSeen.ToUnixTimeSeconds(),
	    FirstSeen = domain.FirstSeen.ToUnixTimeSeconds()
        };
    }

    /// <summary>
    /// Converts this entity instance into a strongly-typed domain model.
    /// </summary>
    public DiscoveredLanDevice ToDomain()
    {
        return new DiscoveredLanDevice(
            IPAddress.Parse(IpAddress),
            MacAddress,
            HostName,
            DateTimeOffset.FromUnixTimeSeconds(LastSeen),
	    DateTimeOffset.FromUnixTimeSeconds(FirstSeen)
        );
    }
}
