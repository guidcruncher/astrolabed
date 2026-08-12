using System.Net;
using System.Net.NetworkInformation;

namespace Astrolabed.Dhcp;

public sealed class DhcpLease
{
    public PhysicalAddress Mac { get; set; } = default!;
    public IPAddress Ip { get; set; } = default!;
    public DateTimeOffset ExpiresAt { get; set; }
}

