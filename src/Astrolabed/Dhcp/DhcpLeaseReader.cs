using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json;

using Astrolabed;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dhcp;

public sealed class DhcpLeaseReader : IDhcpLeaseReader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _path;
    private readonly ILogger<DhcpLeaseReader> _logger;
    private readonly bool _enabled;

    public DhcpLeaseReader(
        IOptions<ServerOptions> options,
        ILogger<DhcpLeaseReader> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var config = options.Value ?? throw new ArgumentNullException(nameof(options));
        ArgumentException.ThrowIfNullOrWhiteSpace(config.Dhcp.LeaseStorePath);

        _enabled = config.Dhcp.Enabled;
        _path = config.Dhcp.LeaseStorePath;
    }

    public bool Enabled()
    {
        return _enabled;
    }

    public async Task<IReadOnlyList<DhcpLease>> GetAllLeasesAsync(CancellationToken cancellationToken = default)
    {
        if (!Enabled()) { return new List<DhcpLease>(); }

        try
        {
            if (!File.Exists(_path))
            {
                _logger.LogInformation("Lease file does not exist at {Path}. Returning empty list.", _path);
                return Array.Empty<DhcpLease>();
            }

            await using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, true);
            var dto = await JsonSerializer.DeserializeAsync<DhcpLeaseStoreDto>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false);

            if (dto?.Leases == null || dto.Leases.Count == 0)
            {
                return Array.Empty<DhcpLease>();
            }

            var leases = new List<DhcpLease>(dto.Leases.Count);
            foreach (var leaseDto in dto.Leases)
            {
                if (TryParseLease(leaseDto, out var lease))
                {
                    leases.Add(lease);
                }
            }

            return leases.AsReadOnly();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading or parsing DHCP lease file at {Path}", _path);
            throw;
        }
    }

    public async Task<DhcpLease?> GetLeaseByIpAsync(IPAddress ip, CancellationToken cancellationToken = default)
    {
        if (!Enabled()) { return null; }

        ArgumentNullException.ThrowIfNull(ip);

        var allLeases = await GetAllLeasesAsync(cancellationToken).ConfigureAwait(false);
        return allLeases.FirstOrDefault(l => l.Ip.Equals(ip));
    }

    private bool TryParseLease(DhcpLeaseDto dto, out DhcpLease lease)
    {
        lease = default!;

        if (dto == null) { return false; }

        if (!PhysicalAddress.TryParse(dto.Mac, out var mac))
        {
            _logger.LogWarning("Invalid MAC address format encountered: {Mac}", dto.Mac);
            return false;
        }

        if (!IPAddress.TryParse(dto.Ip, out var ip))
        {
            _logger.LogWarning("Invalid IP address format encountered: {Ip}", dto.Ip);
            return false;
        }

        lease = new DhcpLease
        {
            Mac = mac,
            Ip = ip,
            ClientName = dto.ClientName ?? "",
            VendorClassIdentifier = dto.VendorClassIdentifier ?? "",
            ExpiresAt = dto.ExpiresAt
        };

        return true;
    }
}
