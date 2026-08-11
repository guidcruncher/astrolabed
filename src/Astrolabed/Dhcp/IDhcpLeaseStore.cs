using System.Net;
using System.Net.NetworkInformation;

namespace Astrolabed.Dhcp;

public interface IDhcpLeaseStore
{
    Task LoadAsync();

    Task SaveAsync();

    IEnumerable<DhcpLease> GetActiveLeases();

    Task SaveAsync(DhcpLease lease);

    Task RemoveAsync(PhysicalAddress mac);

    IEnumerable<IPAddress> GetBadIps();

    Task AddBadIpAsync(IPAddress ip);

    Task RemoveBadIpAsync(IPAddress ip);
}
