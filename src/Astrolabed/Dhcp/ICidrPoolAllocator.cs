using System.Net;
using System.Net.NetworkInformation;

namespace Astrolabed.Dhcp;

public interface ICidrPoolAllocator
{
    bool IsInPool(IPAddress ip);
    IEnumerable<IPAddress> AllocationSequence(IEnumerable<IPAddress> used);
    IPAddress? Allocate(IEnumerable<IPAddress> used);
}
