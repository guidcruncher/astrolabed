using System.Net;

namespace Astrolabed.Data.Models;

/// <summary>
/// Domain model record representing a local network device discovered via passive traffic analysis or active probing.
/// </summary>
/// <param name="IpAddress">The primary assigned IP address of the device.</param>
/// <param name="MacAddress">The physical hardware MAC address string of the network adapter.</param>
/// <param name="HostName">The resolved or declared DNS hostname of the device, if available.</param>
/// <param name="LastSeen">The UTC timestamp indicating when device activity was most recently recorded.</param>
/// <param name="FirstSeen">The UTC timestamp indicating when the device was initially observed on the network.</param>
public record DiscoveredLanDevice(
    IPAddress IpAddress,
    string MacAddress,
    string? HostName,
    DateTimeOffset LastSeen,
    DateTimeOffset FirstSeen
);
