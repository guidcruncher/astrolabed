using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Astrolabed.Dhcp;

public sealed class JsonDhcpLeaseStore : IDhcpLeaseStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters =
        {
            new IPAddressJsonConverter(),
            new PhysicalAddressJsonConverter()
        }
    };

    private readonly string _path;
    private readonly ILogger<JsonDhcpLeaseStore> _logger;
    private readonly ConcurrentDictionary<PhysicalAddress, DhcpLease> _leases = new();
    private readonly ConcurrentDictionary<IPAddress, byte> _badIps = new();
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public JsonDhcpLeaseStore(string path)
        : this(path, NullLogger<JsonDhcpLeaseStore>.Instance)
    {
    }

    public JsonDhcpLeaseStore(string path, ILogger<JsonDhcpLeaseStore> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = path;
        _logger = logger ?? NullLogger<JsonDhcpLeaseStore>.Instance;
    }

    public async Task LoadAsync()
    {
        await _fileLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!File.Exists(_path))
            {
                _logger.LogInformation("DHCP lease store file does not exist at {Path}. Initializing empty store.", _path);
                return;
            }

            var json = await File.ReadAllTextAsync(_path).ConfigureAwait(false);
            var dto = JsonSerializer.Deserialize<DhcpLeaseStoreDto>(json, SerializerOptions);
            if (dto == null)
            {
                _logger.LogWarning("Deserialization returned null for DHCP lease store file at {Path}.", _path);
                return;
            }

            _leases.Clear();
            foreach (var l in dto.Leases)
            {
                if (PhysicalAddress.TryParse(l.Mac, out var mac) && IPAddress.TryParse(l.Ip, out var ip))
                {
                    _leases[mac] = new DhcpLease
                    {
                        Mac = mac,
                        Ip = ip,
                        VendorClassIdentifier = l.VendorClassIdentifier ?? "",
                        ClientName = l.ClientName ?? "",
                        ExpiresAt = l.ExpiresAt
                    };
                }
            }

            _badIps.Clear();
            foreach (var ipStr in dto.BadIps)
            {
                if (IPAddress.TryParse(ipStr, out var ip))
                {
                    _badIps[ip] = 0;
                }
            }

            _logger.LogInformation("Loaded {LeaseCount} leases and {BadIpCount} bad IPs from {Path}", _leases.Count, _badIps.Count, _path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading or parsing DHCP lease store file at {Path}", _path);
            throw;
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
        try
        {
            var dto = new DhcpLeaseStoreDto
            {
                Leases = _leases.Values.Select(l => new DhcpLeaseDto
                {
                    Mac = string.Join(":", l.Mac.GetAddressBytes().Select(b => b.ToString("X2"))),
                    Ip = l.Ip.ToString(),
                    ClientName = l.ClientName,
                    ExpiresAt = l.ExpiresAt
                }).ToList(),

                BadIps = _badIps.Keys.Select(ip => ip.ToString()).ToList()
            };

            var json = JsonSerializer.Serialize(dto, SerializerOptions);

            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Atomic file write using a temporary file to avoid partial write corruption
            var tempPath = $"{_path}.tmp";
            await File.WriteAllTextAsync(tempPath, json).ConfigureAwait(false);
            File.Move(tempPath, _path, overwrite: true);

            _logger.LogDebug("Atomically saved DHCP lease store to {Path}", _path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error persisting DHCP lease store to {Path}", _path);
            throw;
        }
    }

    public IEnumerable<DhcpLease> GetActiveLeases()
    {
        var now = DateTimeOffset.UtcNow;
        return _leases.Values.Where(l => l.ExpiresAt > now).ToList();
    }

    public async Task SaveAsync(DhcpLease lease)
    {
        ArgumentNullException.ThrowIfNull(lease);

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
        ArgumentNullException.ThrowIfNull(mac);

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
        ArgumentNullException.ThrowIfNull(ip);

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
        ArgumentNullException.ThrowIfNull(ip);

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
