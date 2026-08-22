using System.Net;
using Astrolabed.Data.Models;

namespace Astrolabed.Data.Repositories;

public interface IDhcpLeaseRepository
{
    Task<DhcpLease?> GetLeaseByMacAsync(byte[] macAddress, CancellationToken cancellationToken = default);
    Task<DhcpLease?> GetLeaseByIpAsync(IPAddress ipAddress, CancellationToken cancellationToken = default);
    Task<DhcpLease> AllocateOrUpdateLeaseAsync(byte[] macAddress, IPAddress requestedIp, TimeSpan duration, CancellationToken cancellationToken = default);
    Task ReleaseLeaseAsync(byte[] macAddress, CancellationToken cancellationToken = default);
}
