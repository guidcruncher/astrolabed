using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace Astrolabed.Dhcp;

public sealed class DhcpLeaseEngine : IDhcpLeaseEngine
{
    private readonly IDhcpLeaseStore _store;
    private readonly ICidrPoolAllocator _pool;
    private readonly SemaphoreSlim _allocationLock = new(1, 1);
    private readonly ConcurrentDictionary<IPAddress, byte> _pendingAllocations = new();

    public DhcpLeaseEngine(IDhcpLeaseStore store, ICidrPoolAllocator pool)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(pool);

        _store = store;
        _pool = pool;
    }

    public DhcpLease? GetActiveLease(PhysicalAddress mac)
    {
        ArgumentNullException.ThrowIfNull(mac);
        return _store.GetActiveLeases().FirstOrDefault(l => l.Mac.Equals(mac));
    }

    public DhcpLease? GetAnyLease(PhysicalAddress mac)
    {
        ArgumentNullException.ThrowIfNull(mac);
        return _store.GetActiveLeases().FirstOrDefault(l => l.Mac.Equals(mac));
    }

    public async Task<DhcpLease> AllocateAsync(string vendorClassIdentifier, string clientName, PhysicalAddress mac, TimeSpan leaseTime)
    {
        ArgumentNullException.ThrowIfNull(mac);

        await _allocationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var existing = GetActiveLease(mac);
            if (existing != null)
            {
                existing.ExpiresAt = DateTimeOffset.UtcNow.Add(leaseTime);
                await _store.SaveAsync(existing).ConfigureAwait(false);
                return existing;
            }

            var activeAndPending = _store.GetActiveLeases()
                .Select(l => l.Ip)
                .Concat(_pendingAllocations.Keys);

            var ip = _pool.Allocate(activeAndPending);
            if (ip == null)
            {
                throw new InvalidOperationException("DHCP pool exhausted");
            }

            var lease = new DhcpLease
            {
                ClientName = clientName,
                VendorClassIdentifier = vendorClassIdentifier,
                Mac = mac,
                Ip = ip,
                ExpiresAt = DateTimeOffset.UtcNow.Add(leaseTime)
            };

            await _store.SaveAsync(lease).ConfigureAwait(false);
            return lease;
        }
        finally
        {
            _allocationLock.Release();
        }
    }

    public async Task<DhcpLease> AllocateWithArpCheckAsync(
        string vendorClassIdentifier,
        string clientName,
        PhysicalAddress mac,
        TimeSpan leaseTime,
        IArpConflictDetector arp)
    {
        ArgumentNullException.ThrowIfNull(mac);
        ArgumentNullException.ThrowIfNull(arp);

        List<IPAddress> candidates = new();
        HashSet<IPAddress> badIps;

        await _allocationLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var existing = GetActiveLease(mac);
            if (existing != null)
            {
                existing.ExpiresAt = DateTimeOffset.UtcNow.Add(leaseTime);
                await _store.SaveAsync(existing).ConfigureAwait(false);
                return existing;
            }

            var previousLease = GetAnyLease(mac);
            if (previousLease != null && !_pendingAllocations.ContainsKey(previousLease.Ip))
            {
                candidates.Add(previousLease.Ip);
            }

            var used = _store.GetActiveLeases().Select(l => l.Ip).Concat(_pendingAllocations.Keys);
            candidates.AddRange(_pool.AllocationSequence(used));

            badIps = _store.GetBadIps().ToHashSet();
        }
        finally
        {
            _allocationLock.Release();
        }

        foreach (var candidate in candidates)
        {
            if (badIps.Contains(candidate))
            {
                continue;
            }

            if (!_pendingAllocations.TryAdd(candidate, 0))
            {
                continue;
            }

            try
            {
                bool conflict = await arp.HasConflictAsync(candidate, TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);

                await _allocationLock.WaitAsync().ConfigureAwait(false);
                try
                {
                    var existingDuringProbe = GetActiveLease(mac);
                    if (existingDuringProbe != null)
                    {
                        existingDuringProbe.ExpiresAt = DateTimeOffset.UtcNow.Add(leaseTime);
                        await _store.SaveAsync(existingDuringProbe).ConfigureAwait(false);
                        return existingDuringProbe;
                    }

                    if (!conflict)
                    {
                        var lease = new DhcpLease
                        {
                            ClientName = clientName,
                            VendorClassIdentifier = vendorClassIdentifier,
                            Mac = mac,
                            Ip = candidate,
                            ExpiresAt = DateTimeOffset.UtcNow.Add(leaseTime)
                        };

                        await _store.SaveAsync(lease).ConfigureAwait(false);
                        return lease;
                    }

                    await _store.AddBadIpAsync(candidate).ConfigureAwait(false);
                }
                finally
                {
                    _allocationLock.Release();
                }
            }
            finally
            {
                _pendingAllocations.TryRemove(candidate, out _);
            }
        }

        throw new InvalidOperationException("DHCP pool exhausted (no conflict-free IPs)");
    }

    public async Task ReleaseAsync(PhysicalAddress mac)
    {
        ArgumentNullException.ThrowIfNull(mac);
        await _store.RemoveAsync(mac).ConfigureAwait(false);
    }

    public async Task DeclineAsync(IPAddress ip)
    {
        ArgumentNullException.ThrowIfNull(ip);
        await _store.AddBadIpAsync(ip).ConfigureAwait(false);
    }
}
