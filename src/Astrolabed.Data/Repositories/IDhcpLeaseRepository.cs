using System.Net;

using Astrolabed.Data.Models;

namespace Astrolabed.Data.Repositories;

public interface IDhcpLeaseRepository
{
    Task<DhcpLease?> GetLeaseByClientIdOrMacAsync(string clientId, string macAddress, CancellationToken cancellationToken = default);
    Task<DhcpLease?> GetLeaseByIpAsync(IPAddress ipAddress, CancellationToken cancellationToken = default);
    Task<bool> IsIpAvailableAsync(IPAddress ipAddress, string clientId, CancellationToken cancellationToken = default);
    Task<DhcpLease> AllocateOrUpdateLeaseAsync(string clientId, string clientName, string macAddress, IPAddress requestedIp, TimeSpan duration, CancellationToken cancellationToken = default);
    Task ReleaseLeaseAsync(string clientId, string macAddress, CancellationToken cancellationToken = default);
}
