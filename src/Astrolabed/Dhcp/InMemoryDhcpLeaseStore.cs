using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;

namespace Astrolabed.Dhcp;

public sealed class InMemoryDhcpLeaseStore : IDhcpLeaseStore
{
    private readonly ConcurrentDictionary<PhysicalAddress, DhcpLease> _leases = new();
    private readonly ConcurrentDictionary<IPAddress, byte> _badIps = new();

    public Task LoadAsync()
    {
        return Task.CompletedTask;
    }

    public Task SaveAsync()
    {
        return Task.CompletedTask;
    }

    public IEnumerable<DhcpLease> GetActiveLeases()
    {
        var now = DateTimeOffset.UtcNow;
        return _leases.Values.Where(l => l.ExpiresAt > now).ToList();
    }

    public Task SaveAsync(DhcpLease lease)
    {
        _leases[lease.Mac] = lease;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(PhysicalAddress mac)
    {
        _leases.TryRemove(mac, out _);
        return Task.CompletedTask;
    }

    public IEnumerable<IPAddress> GetBadIps() => _badIps.Keys.ToList();

    public Task AddBadIpAsync(IPAddress ip)
    {
        _badIps[ip] = 0;
        return Task.CompletedTask;
    }

    public Task RemoveBadIpAsync(IPAddress ip)
    {
        _badIps.TryRemove(ip, out _);
        return Task.CompletedTask;
    }
}
