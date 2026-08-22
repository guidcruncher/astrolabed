using System.Collections.Concurrent;
using System.Net;

using Astrolabed.Data.Models;

namespace Astrolabed.Data.Repositories;


public class InMemoryDhcpLeaseRepository : IDhcpLeaseRepository
{
    private readonly ConcurrentDictionary<string, DhcpLease> _leasesByClientId = new(StringComparer.OrdinalIgnoreCase);

    public Task<DhcpLease?> GetLeaseByClientIdOrMacAsync(string clientId, string macAddress, CancellationToken cancellationToken = default)
    {
        if (_leasesByClientId.TryGetValue(clientId, out var lease))
        {
            return Task.FromResult<DhcpLease?>(lease);
        }

        var leaseByMac = _leasesByClientId.Values.FirstOrDefault(l => l.MacAddress.Equals(macAddress, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(leaseByMac);
    }

    public Task<DhcpLease?> GetLeaseByIpAsync(IPAddress ipAddress, CancellationToken cancellationToken = default)
    {
        var lease = _leasesByClientId.Values.FirstOrDefault(l => l.IpAddress.Equals(ipAddress) && l.IsActive);
        return Task.FromResult(lease);
    }

    public Task<bool> IsIpAvailableAsync(IPAddress ipAddress, string clientId, CancellationToken cancellationToken = default)
    {
        var existingLease = _leasesByClientId.Values.FirstOrDefault(l => l.IpAddress.Equals(ipAddress) && l.IsActive);
        if (existingLease == null)
        {
            return Task.FromResult(true);
        }

        bool belongsToClient = existingLease.ClientId.Equals(clientId, StringComparison.OrdinalIgnoreCase);
        return Task.FromResult(belongsToClient);
    }

    public Task<DhcpLease> AllocateOrUpdateLeaseAsync(string clientId, string clientName, string macAddress, IPAddress requestedIp, TimeSpan duration, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var lease = new DhcpLease
        {
            ClientId = clientId,
            ClientName = clientName,
            MacAddress = macAddress,
            IpAddress = requestedIp,
            LeaseStartTime = now,
            LeaseEndTime = now.Add(duration),
            IsActive = true
        };

        _leasesByClientId[clientId] = lease;
        return Task.FromResult(lease);
    }

    public Task ReleaseLeaseAsync(string clientId, string macAddress, CancellationToken cancellationToken = default)
    {
        if (_leasesByClientId.TryGetValue(clientId, out var leaseByClient))
        {
            leaseByClient.IsActive = false;
        }
        else
        {
            var leaseByMac = _leasesByClientId.Values.FirstOrDefault(l => l.MacAddress.Equals(macAddress, StringComparison.OrdinalIgnoreCase));
            if (leaseByMac != null)
            {
                leaseByMac.IsActive = false;
            }
        }

        return Task.CompletedTask;
    }
}
