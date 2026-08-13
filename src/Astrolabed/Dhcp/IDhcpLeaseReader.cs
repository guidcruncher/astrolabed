using System.Net;

namespace Astrolabed.Dhcp;

public interface IDhcpLeaseReader
{
    bool Enabled();

    /// <summary>
    /// Reads and returns all leases present in the JSON file.
    /// </summary>
    Task<IReadOnlyList<DhcpLease>> GetAllLeasesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for a lease entry matching the specified IP address.
    /// </summary>
    Task<DhcpLease?> GetLeaseByIpAsync(IPAddress ip, CancellationToken cancellationToken = default);
}
