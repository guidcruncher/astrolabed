using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json;

namespace Astrolabed.Dhcp;

public sealed class JsonDhcpLeaseStore : IDhcpLeaseStore
{
    private readonly string _path;
    private readonly ConcurrentDictionary<PhysicalAddress, DhcpLease> _leases = new();
    private readonly ConcurrentDictionary<IPAddress, byte> _badIps = new();
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public JsonDhcpLeaseStore(string path)
    {
        _path = path;
    }

    public async Task LoadAsync()
    {
        await _fileLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!File.Exists(_path))
                return;

            var json = await File.ReadAllTextAsync(_path).ConfigureAwait(false);

            var dto = JsonSerializer.Deserialize<DhcpLeaseStoreDto>(json);
            if (dto == null)
                return;

            _leases.Clear();
            foreach (var l in dto.Leases)
            {
                var mac = new PhysicalAddress(l.Mac);
                _leases[mac] = new DhcpLease
                {
                    Mac = mac,
                    Ip = new IPAddress(l.Ip),
                    ExpiresAt = l.ExpiresAt
                };
            }

            _badIps.Clear();
            foreach (var ip in dto.BadIps)
            {
                _badIps[new IPAddress(ip)] = 0;
            }
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task SaveAsync()
    {
        await _fileLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await SaveInternalUnsafeAsync().ConfigureAwait(false);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task SaveInternalUnsafeAsync()
    {
        var dto = new DhcpLeaseStoreDto
        {
            Leases = _leases.Values.Select(l => new DhcpLeaseDto
            {
                Mac = l.Mac.GetAddressBytes(),
                Ip = l.Ip.GetAddressBytes(),
                ExpiresAt = l.ExpiresAt
            }).ToList(),

            BadIps = _badIps.Keys.Select(ip => ip.GetAddressBytes()).ToList()
        };

        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await File.WriteAllTextAsync(_path, json).ConfigureAwait(false);
    }

    public IEnumerable<DhcpLease> GetActiveLeases()
    {
        var now = DateTimeOffset.UtcNow;
        return _leases.Values.Where(l => l.ExpiresAt > now).ToList();
    }

    public async Task SaveAsync(DhcpLease lease)
    {
        _leases[lease.Mac] = lease;

        await _fileLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await SaveInternalUnsafeAsync().ConfigureAwait(false);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task RemoveAsync(PhysicalAddress mac)
    {
        _leases.TryRemove(mac, out _);

        await _fileLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await SaveInternalUnsafeAsync().ConfigureAwait(false);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public IEnumerable<IPAddress> GetBadIps() => _badIps.Keys.ToList();

    public async Task AddBadIpAsync(IPAddress ip)
    {
        _badIps[ip] = 0;

        await _fileLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await SaveInternalUnsafeAsync().ConfigureAwait(false);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task RemoveBadIpAsync(IPAddress ip)
    {
        _badIps.TryRemove(ip, out _);

        await _fileLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await SaveInternalUnsafeAsync().ConfigureAwait(false);
        }
        finally
        {
            _fileLock.Release();
        }
    }
}
