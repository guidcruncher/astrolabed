using System.Net;
using System.Net.NetworkInformation;

namespace Astrolabed.Dhcp;

public sealed class DhcpLease
{
    public string ClientName { get; set; } = "";
    public string VendorClassIdentifier { get; set; } = "";
    public PhysicalAddress Mac { get; set; } = default!;
    public IPAddress Ip { get; set; } = default!;
    public DateTimeOffset ExpiresAt { get; set; }
}

