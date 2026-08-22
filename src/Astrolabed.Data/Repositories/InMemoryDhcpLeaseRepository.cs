using System.Collections.Concurrent;
using System.Net;
using Astrolabed.Data.Models;

namespace Astrolabed.Data.Repositories;

public class InMemoryDhcpLeaseRepository : IDhcpLeaseRepository
{
    private readonly ConcurrentDictionary<string, DhcpLease> _leasesByMac = new(StringComparer.OrdinalIgnoreCase);

    public Task<DhcpLease?> GetLeaseByMacAsync(byte[] macAddress, CancellationToken cancellationToken = default)
    {
        string key = Convert.ToHexString(macAddress);
        _leasesByMac.TryGetValue(key, out var lease);
        return Task.FromResult(lease);
    }

    public Task<DhcpLease?> GetLeaseByIpAsync(IPAddress ipAddress, CancellationToken cancellationToken = default)
    {
        var lease = _leasesByMac.Values.FirstOrDefault(l => l.IpAddress.Equals(ipAddress) && l.IsActive);
        return Task.FromResult(lease);
    }

    public Task<DhcpLease> AllocateOrUpdateLeaseAsync(byte[] macAddress, IPAddress requestedIp, TimeSpan duration, CancellationToken cancellationToken = default)
    {
        string key = Convert.ToHexString(macAddress);
        var now = DateTime.UtcNow;

        var lease = new DhcpLease
        {
            ClientId = key,
            MacAddress = macAddress,
            IpAddress = requestedIp,
            LeaseStartTime = now,
            LeaseEndTime = now.Add(duration),
            IsActive = true
        };

        _leasesByMac[key] = lease;
        return Task.FromResult(lease);
    }

    public Task ReleaseLeaseAsync(byte[] macAddress, CancellationToken cancellationToken = default)
    {
        string key = Convert.ToHexString(macAddress);
        if (_leasesByMac.TryGetValue(key, out var lease))
        {
            lease.IsActive = false;
        }
        return Task.CompletedTask;
    }
}
