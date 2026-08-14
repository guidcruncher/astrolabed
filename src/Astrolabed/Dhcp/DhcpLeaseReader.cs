using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json;

using Astrolabed;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dhcp;

public sealed partial class DhcpLeaseReader : IDhcpLeaseReader
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

        LogInitialized(_logger, _path, _enabled);
    }

    public bool Enabled()
    {
        return _enabled;
    }

    public async Task<IReadOnlyList<DhcpLease>> GetAllLeasesAsync(CancellationToken cancellationToken = default)
    {
        if (!Enabled())
        {
            LogReaderDisabled(_logger);
            return new List<DhcpLease>();
        }

        try
        {
            if (!File.Exists(_path))
            {
                LogFileNotFound(_logger, _path);
                return Array.Empty<DhcpLease>();
            }

            LogReadingLeaseFile(_logger, _path);

            await using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, true);
            var dto = await JsonSerializer.DeserializeAsync<DhcpLeaseStoreDto>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false);

            if (dto?.Leases == null || dto.Leases.Count == 0)
            {
                LogEmptyLeaseStore(_logger, _path);
                return Array.Empty<DhcpLease>();
            }

            var leases = new List<DhcpLease>(dto.Leases.Count);
            var skippedCount = 0;

            foreach (var leaseDto in dto.Leases)
            {
                if (TryParseLease(leaseDto, out var lease))
                {
                    leases.Add(lease);
                }
                else
                {
                    skippedCount++;
                }
            }

            LogSuccessfullyReadLeases(_logger, leases.Count, skippedCount, _path);
            return leases.AsReadOnly();
        }
        catch (OperationCanceledException ex)
        {
            LogReadCanceled(_logger, _path, ex);
            throw;
        }
        catch (Exception ex)
        {
            LogErrorReadingLeases(_logger, _path, ex);
            throw;
        }
    }

    public async Task<DhcpLease?> GetLeaseByIpAsync(IPAddress ip, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ip);

        if (!Enabled())
        {
            LogReaderDisabledForIpLookup(_logger, ip);
            return null;
        }

        LogSearchingLeaseByIp(_logger, ip);

        var allLeases = await GetAllLeasesAsync(cancellationToken).ConfigureAwait(false);
        var lease = allLeases.FirstOrDefault(l => l.Ip.Equals(ip));

        if (lease is not null)
        {
            LogLeaseFoundByIp(_logger, ip, lease.Mac);
        }
        else
        {
            LogLeaseNotFoundByIp(_logger, ip);
        }

        return lease;
    }

    private bool TryParseLease(DhcpLeaseDto dto, out DhcpLease lease)
    {
        lease = default!;

        if (dto == null)
        {
            LogNullLeaseDtoEncountered(_logger);
            return false;
        }

        if (!PhysicalAddress.TryParse(dto.Mac, out var mac))
        {
            LogInvalidMacAddressFormat(_logger, dto.Mac);
            return false;
        }

        if (!IPAddress.TryParse(dto.Ip, out var ip))
        {
            LogInvalidIpAddressFormat(_logger, dto.Ip);
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

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Initialized DhcpLeaseReader with Path: {Path}, Enabled: {Enabled}")]
    private static partial void LogInitialized(ILogger logger, string path, bool enabled);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "DHCP lease reading is disabled. Skipping request.")]
    private static partial void LogReaderDisabled(ILogger logger);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Lease file does not exist at {Path}. Returning empty list.")]
    private static partial void LogFileNotFound(ILogger logger, string path);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Reading DHCP lease file from {Path}")]
    private static partial void LogReadingLeaseFile(ILogger logger, string path);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "Lease file at {Path} contains no leases.")]
    private static partial void LogEmptyLeaseStore(ILogger logger, string path);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "Successfully read {ValidCount} valid DHCP lease(s) (Skipped: {SkippedCount}) from {Path}")]
    private static partial void LogSuccessfullyReadLeases(ILogger logger, int validCount, int skippedCount, string path);

    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "DHCP lease file read was canceled at {Path}")]
    private static partial void LogReadCanceled(ILogger logger, string path, Exception exception);

    [LoggerMessage(EventId = 8, Level = LogLevel.Error, Message = "Error reading or parsing DHCP lease file at {Path}")]
    private static partial void LogErrorReadingLeases(ILogger logger, string path, Exception exception);

    [LoggerMessage(EventId = 9, Level = LogLevel.Debug, Message = "DHCP lease reading is disabled. Cannot query IP {Ip}")]
    private static partial void LogReaderDisabledForIpLookup(ILogger logger, IPAddress ip);

    [LoggerMessage(EventId = 10, Level = LogLevel.Debug, Message = "Searching for DHCP lease with IP: {Ip}")]
    private static partial void LogSearchingLeaseByIp(ILogger logger, IPAddress ip);

    [LoggerMessage(EventId = 11, Level = LogLevel.Debug, Message = "Found DHCP lease for IP {Ip} with MAC {Mac}")]
    private static partial void LogLeaseFoundByIp(ILogger logger, IPAddress ip, PhysicalAddress mac);

    [LoggerMessage(EventId = 12, Level = LogLevel.Debug, Message = "No DHCP lease found for IP {Ip}")]
    private static partial void LogLeaseNotFoundByIp(ILogger logger, IPAddress ip);

    [LoggerMessage(EventId = 13, Level = LogLevel.Warning, Message = "Encountered null lease DTO during parsing")]
    private static partial void LogNullLeaseDtoEncountered(ILogger logger);

    [LoggerMessage(EventId = 14, Level = LogLevel.Warning, Message = "Invalid MAC address format encountered: {Mac}")]
    private static partial void LogInvalidMacAddressFormat(ILogger logger, string? mac);

    [LoggerMessage(EventId = 15, Level = LogLevel.Warning, Message = "Invalid IP address format encountered: {Ip}")]
    private static partial void LogInvalidIpAddressFormat(ILogger logger, string? ip);
}
