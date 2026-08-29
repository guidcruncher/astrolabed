using System.Net;

using Astrolabed.Core.Network;

namespace Astrolabed.Data.Models;

/// <summary>
/// Database entity representation for a discovered LAN device.
/// Maps directly to the underlying SQL table structure where IP addresses are stored as strings
/// and timestamps are stored as Unix epoch seconds (bigint/long).
/// </summary>
public sealed class DiscoveredLanDeviceEntity
{
    /// <summary>
    /// Gets or sets the primary string IP address column.
    /// </summary>
    public required string IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the calculated reverse DNS pointer query address column.
    /// </summary>
    public required string PtrAddress { get; set; }

    /// <summary>
    /// Gets or sets the network adapter physical MAC address string column.
    /// </summary>
    public required string MacAddress { get; set; }

    /// <summary>
    /// Gets or sets the resolved hostname column string.
    /// </summary>
    public string? HostName { get; set; }

    /// <summary>
    /// Gets or sets the most recent observation UTC timestamp expressed in Unix epoch seconds.
    /// </summary>
    public required long LastSeen { get; set; }

    /// <summary>
    /// Gets or sets the initial observation UTC timestamp expressed in Unix epoch seconds.
    /// </summary>
    public required long FirstSeen { get; set; }

    /// <summary>
    /// Gets or sets the Vendor
    /// </summary>
    public required string Vendor { get; set; }

    /// <summary>
    /// Gets or sets the Device Type
    /// </summary>
    public required string DeviceType { get; set; }

    /// <summary>
    /// Creates a persistence entity instance from a domain record.
    /// </summary>
    /// <param name="domain">The domain record to convert.</param>
    /// <returns>A new <see cref="DiscoveredLanDeviceEntity"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="domain"/> is <c>null</c>.</exception>
    public static DiscoveredLanDeviceEntity FromDomain(DiscoveredLanDevice domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        return new DiscoveredLanDeviceEntity
        {
            IpAddress = domain.IpAddress.ToString(),
            PtrAddress = domain.IpAddress.ToPtrFormat(),
            MacAddress = domain.MacAddress,
            HostName = domain.HostName,
            LastSeen = domain.LastSeen.ToUnixTimeSeconds(),
            FirstSeen = domain.FirstSeen.ToUnixTimeSeconds(),
            Vendor = domain.Vendor,
            DeviceType = domain.DeviceType
        };
    }


    /// <summary>
    /// Converts this entity instance into a strongly-typed domain model.
    /// </summary>
    /// <returns>A populated <see cref="DiscoveredLanDevice"/> instance.</returns>
    public DiscoveredLanDevice ToDomain()
    {
        return new DiscoveredLanDevice(
            IPAddress.Parse(IpAddress),
            MacAddress,
            HostName,
            DateTimeOffset.FromUnixTimeSeconds(LastSeen),
            DateTimeOffset.FromUnixTimeSeconds(FirstSeen),
        Vendor,
        DeviceType
        );
    }
}
