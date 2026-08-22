using System.Net;

namespace Astrolabed.Data.Models;

public record DiscoveredLanDevice(
        IPAddress IpAddress,
        string MacAddress,
        string? HostName,
        DateTimeOffset LastSeen
);
