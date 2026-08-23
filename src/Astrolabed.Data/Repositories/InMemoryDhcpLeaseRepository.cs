using System.Collections.Concurrent;
using System.Net;

using Astrolabed.Core.Network;
using Astrolabed.Data.Models;

namespace Astrolabed.Data.Repositories;

/// <summary>
/// High-performance, thread-safe in-memory repository for managing <see cref="DhcpLease"/> records.
/// Uses secondary index dictionaries to achieve O(1) constant time lookups across Client ID, MAC address, and IP address.
/// </summary>
/// <remarks>
/// Targets .NET 10 standards. Guarantees memory safety and concurrent protection by storing immutable lease snapshots.
/// </remarks>
public sealed class InMemoryDhcpLeaseRepository : IDhcpLeaseRepository
{
    private readonly ConcurrentDictionary<string, DhcpLease> _leasesByClientId = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, DhcpLease> _leasesByMac = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<IPAddress, DhcpLease> _leasesByIp = new();

    /// <inheritdoc />
    public Task<DhcpLease?> GetLeaseByClientIdOrMacAsync(
        string clientId,
        string macAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(macAddress);

        if (_leasesByClientId.TryGetValue(clientId, out DhcpLease? leaseByClient))
        {
            return Task.FromResult<DhcpLease?>(CloneLease(leaseByClient));
        }

        string formattedMac = MacAddressFormatter.Format(macAddress);
        if (_leasesByMac.TryGetValue(formattedMac, out DhcpLease? leaseByMac))
        {
            return Task.FromResult<DhcpLease?>(CloneLease(leaseByMac));
        }

        return Task.FromResult<DhcpLease?>(null);
    }

    /// <inheritdoc />
    public Task<DhcpLease?> GetLeaseByIpAsync(
        IPAddress ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ipAddress);

        if (_leasesByIp.TryGetValue(ipAddress, out DhcpLease? lease) && lease.IsActive)
        {
            return Task.FromResult<DhcpLease?>(CloneLease(lease));
        }

        return Task.FromResult<DhcpLease?>(null);
    }

    /// <inheritdoc />
    public Task<bool> IsIpAvailableAsync(
        IPAddress ipAddress,
        string clientId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ipAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

        if (!_leasesByIp.TryGetValue(ipAddress, out DhcpLease? existingLease) || !existingLease.IsActive)
        {
            return Task.FromResult(true);
        }

        bool belongsToClient = existingLease.ClientId.Equals(clientId, StringComparison.OrdinalIgnoreCase);
        return Task.FromResult(belongsToClient);
    }

    /// <inheritdoc />
    public Task<DhcpLease> AllocateOrUpdateLeaseAsync(
        string clientId,
        string clientName,
        string macAddress,
        IPAddress requestedIp,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentNullException.ThrowIfNull(clientName);
        ArgumentException.ThrowIfNullOrWhiteSpace(macAddress);
        ArgumentNullException.ThrowIfNull(requestedIp);

        string formattedMac = MacAddressFormatter.Format(macAddress);
        DateTime now = DateTime.UtcNow;

        var lease = new DhcpLease
        {
            ClientId = clientId,
            ClientName = clientName,
            MacAddress = formattedMac,
            IpAddress = requestedIp,
            LeaseStartTime = now,
            LeaseEndTime = now.Add(duration),
            IsActive = true
        };

        DhcpLease snapshot = CloneLease(lease);

        _leasesByClientId[clientId] = snapshot;
        _leasesByMac[formattedMac] = snapshot;
        _leasesByIp[requestedIp] = snapshot;

        return Task.FromResult(CloneLease(snapshot));
    }

    /// <inheritdoc />
    public Task ReleaseLeaseAsync(
        string clientId,
        string macAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(macAddress);

        string formattedMac = MacAddressFormatter.Format(macAddress);

        if (_leasesByClientId.TryGetValue(clientId, out DhcpLease? existingLease))
        {
            DeactivateLease(existingLease);
        }
        else if (_leasesByMac.TryGetValue(formattedMac, out existingLease))
        {
            DeactivateLease(existingLease);
        }

        return Task.CompletedTask;
    }

    private void DeactivateLease(DhcpLease lease)
    {
        var deactivatedLease = new DhcpLease
        {
            ClientId = lease.ClientId,
            ClientName = lease.ClientName,
            MacAddress = lease.MacAddress,
            IpAddress = lease.IpAddress,
            LeaseStartTime = lease.LeaseStartTime,
            LeaseEndTime = lease.LeaseEndTime,
            IsActive = false
        };

        _leasesByClientId[lease.ClientId] = deactivatedLease;
        _leasesByMac[lease.MacAddress] = deactivatedLease;
        _leasesByIp[lease.IpAddress] = deactivatedLease;
    }

    private static DhcpLease CloneLease(DhcpLease lease)
    {
        return new DhcpLease
        {
            ClientId = lease.ClientId,
            ClientName = lease.ClientName,
            MacAddress = lease.MacAddress,
            IpAddress = lease.IpAddress,
            LeaseStartTime = lease.LeaseStartTime,
            LeaseEndTime = lease.LeaseEndTime,
            IsActive = lease.IsActive
        };
    }
}
