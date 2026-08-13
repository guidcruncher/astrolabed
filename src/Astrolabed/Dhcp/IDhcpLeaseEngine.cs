using System.Net;
using System.Net.NetworkInformation;

namespace Astrolabed.Dhcp;

public interface IDhcpLeaseEngine
{
    DhcpLease? GetActiveLease(PhysicalAddress mac);
    DhcpLease? GetAnyLease(PhysicalAddress mac);
    Task<DhcpLease> AllocateAsync(string vendorClassIdentifier, string clientName, PhysicalAddress mac, TimeSpan leaseTime);
    Task<DhcpLease> AllocateWithArpCheckAsync(string vendorClassIdentifier, string clientName, PhysicalAddress mac, TimeSpan leaseTime, IArpConflictDetector arp);
    Task ReleaseAsync(PhysicalAddress mac);
    Task DeclineAsync(IPAddress ip);
}
