using System.Data;
using System.Net;

using Astrolabed.Data.Models;
using Astrolabed.Data.Options;
using Astrolabed.Data.Pagination;

using Dapper;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Data.Repositories;

/// <summary>
/// Dapper implementation for managing <see cref="DiscoveredLanDevice"/> persistence, 
/// mapping queries through <see cref="DiscoveredLanDeviceEntity"/> to maintain exact SQL representations.
/// </summary>
public sealed class DapperDiscoveredLanDeviceRepository : IDiscoveredLanDeviceRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly DatabaseOptions _databaseOptions;
    private readonly ILogger<DapperDiscoveredLanDeviceRepository> _logger;

    public DapperDiscoveredLanDeviceRepository(
        IDbConnectionFactory connectionFactory,
        IOptions<DatabaseOptions> databaseOptions,
        ILogger<DapperDiscoveredLanDeviceRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        ArgumentNullException.ThrowIfNull(databaseOptions);
        ArgumentNullException.ThrowIfNull(logger);

        _connectionFactory = connectionFactory;
        _databaseOptions = databaseOptions.Value;
        _logger = logger;
    }

    public async Task UpsertAsync(DiscoveredLanDevice device, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);

        const string sql = """
            INSERT INTO discovered_lan_devices (
                mac_address, ip_address, ptr_address, host_name, last_seen, first_seen
            ) VALUES (
                @MacAddress, @IpAddress, @ptrAddress, @HostName, @LastSeen, @FirstSeen
            )
            ON CONFLICT (mac_address) DO UPDATE SET
                ip_address = EXCLUDED.ip_address,
                ptr_address = EXCLUDED.ptr_address,
                host_name = EXCLUDED.host_name,
                last_seen = EXCLUDED.last_seen;
            """;

        long firstSeenEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        DiscoveredLanDeviceEntity entity = DiscoveredLanDeviceEntity.FromDomain(device);

        using IDbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        _logger.LogDebug("Upserting LAN device record for MAC {MacAddress}", device.MacAddress);

        var command = new CommandDefinition(
            sql,
            new
            {
                entity.MacAddress,
                entity.IpAddress,
                entity.PtrAddress,
                entity.HostName,
                entity.LastSeen,
                FirstSeen = firstSeenEpoch
            },
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);

        _logger.LogInformation("Successfully upserted LAN device record for MAC {MacAddress}", device.MacAddress);
    }

    public async Task BulkUpsertAsync(IEnumerable<DiscoveredLanDevice> devices, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(devices);

        long firstSeenEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var parameters = devices.Select(device =>
        {
            DiscoveredLanDeviceEntity entity = DiscoveredLanDeviceEntity.FromDomain(device);
            return new
            {
                entity.MacAddress,
                entity.IpAddress,
                entity.PtrAddress,
                entity.HostName,
                entity.LastSeen,
                FirstSeen = firstSeenEpoch
            };
        }).ToList();

        if (parameters.Count == 0)
        {
            _logger.LogDebug("Bulk upsert skipped as the provided collection contains no items");
            return;
        }

        const string sql = """
            INSERT INTO discovered_lan_devices (
                mac_address, ip_address, ptr_address, host_name, last_seen, first_seen
            ) VALUES (
                @MacAddress, @IpAddress, @PtrAddress, @HostName, @LastSeen, @FirstSeen
            )
            ON CONFLICT (mac_address) DO UPDATE SET
                ip_address = EXCLUDED.ip_address,
                ptr_address = EXCLUDED.ptr_address,
                host_name = EXCLUDED.host_name,
                last_seen = EXCLUDED.last_seen;
            """;

        using IDbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        _logger.LogDebug("Executing bulk upsert for {Count} LAN device records", parameters.Count);

        var command = new CommandDefinition(
            sql,
            parameters,
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        int rowsAffected = await connection.ExecuteAsync(command);

        _logger.LogInformation(
            "Successfully completed bulk upsert for {RequestedCount} devices ({RowsAffected} rows modified)",
            parameters.Count,
            rowsAffected);
    }

    public async Task<DiscoveredLanDevice?> GetByPtrAddressAsync(string ptrAddress, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ptrAddress);

        const string sql = """
            SELECT ip_address AS IpAddress,
                   mac_address AS MacAddress,
                   host_name AS HostName,
                   last_seen AS LastSeen,
                   first_seen AS FirstSeen
            FROM discovered_lan_devices
            WHERE ptr_address = @PtrAddress;
            """;

        using IDbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        _logger.LogDebug("Fetching LAN device record for PTR {PrrAddress}", ptrAddress);

        var command = new CommandDefinition(
            sql,
            new { PtrAddress = ptrAddress },
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        DiscoveredLanDeviceEntity? entity = await connection.QuerySingleOrDefaultAsync<DiscoveredLanDeviceEntity>(command);

        return entity?.ToDomain();
    }

    public async Task<DiscoveredLanDevice?> GetByMacAddressAsync(string macAddress, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(macAddress);

        const string sql = """
            SELECT ip_address AS IpAddress,
                   mac_address AS MacAddress,
                   host_name AS HostName,
                   last_seen AS LastSeen,
                   first_seen AS FirstSeen
            FROM discovered_lan_devices
            WHERE mac_address = @MacAddress;
            """;

        using IDbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        _logger.LogDebug("Fetching LAN device record for MAC {MacAddress}", macAddress);

        var command = new CommandDefinition(
            sql,
            new { MacAddress = macAddress },
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        DiscoveredLanDeviceEntity? entity = await connection.QuerySingleOrDefaultAsync<DiscoveredLanDeviceEntity>(command);

        return entity?.ToDomain();
    }

    public async Task<DiscoveredLanDevice?> GetByIpAddressAsync(IPAddress ipAddress, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ipAddress);

        const string sql = """
            SELECT ip_address AS IpAddress,
                   mac_address AS MacAddress,
                   host_name AS HostName,
                   last_seen AS LastSeen,
                   first_seen AS FirstSeen
            FROM discovered_lan_devices
            WHERE ip_address = @IpAddress;
            """;

        using IDbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        string ipString = ipAddress.ToString();
        _logger.LogDebug("Fetching LAN device record for IP {IpAddress}", ipString);

        var command = new CommandDefinition(
            sql,
            new { IpAddress = ipString },
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        DiscoveredLanDeviceEntity? entity = await connection.QuerySingleOrDefaultAsync<DiscoveredLanDeviceEntity>(command);

        return entity?.ToDomain();
    }

    public async Task<PagedResult<DiscoveredLanDevice>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        int targetPage = pageNumber < 1 ? 1 : pageNumber;
        int targetSize = pageSize < 1 ? 10 : Math.Min(pageSize, 100);
        int offset = (targetPage - 1) * targetSize;

        const string sql = """
            SELECT COUNT(1) FROM discovered_lan_devices;

            SELECT ip_address AS IpAddress,
                   mac_address AS MacAddress,
                   host_name AS HostName,
                   last_seen AS LastSeen,
                   first_seen AS FirstSeen
            FROM discovered_lan_devices
            ORDER BY last_seen DESC
            LIMIT @PageSize OFFSET @Offset;
            """;

        using IDbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        _logger.LogDebug(
            "Executing paged SELECT for LAN devices. PageNumber: {PageNumber}, PageSize: {PageSize}",
            targetPage,
            targetSize);

        var command = new CommandDefinition(
            sql,
            new { PageSize = targetSize, Offset = offset },
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        using SqlMapper.GridReader gridReader = await connection.QueryMultipleAsync(command);

        long totalCount = await gridReader.ReadSingleAsync<long>();
        IEnumerable<DiscoveredLanDeviceEntity> entities = await gridReader.ReadAsync<DiscoveredLanDeviceEntity>();

        List<DiscoveredLanDevice> items = entities.Select(e => e.ToDomain()).ToList();

        _logger.LogInformation(
            "Retrieved page {PageNumber} with {Count} LAN device records (Total dataset size: {TotalCount})",
            targetPage,
            items.Count,
            totalCount);

        return PagedResult<DiscoveredLanDevice>.Create(items, totalCount, targetPage, targetSize);
    }

    public async Task<bool> DeleteByMacAddressAsync(string macAddress, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(macAddress);

        const string sql = "DELETE FROM discovered_lan_devices WHERE mac_address = @MacAddress;";

        using IDbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        _logger.LogDebug("Deleting LAN device record for MAC {MacAddress}", macAddress);

        var command = new CommandDefinition(
            sql,
            new { MacAddress = macAddress },
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        int rowsAffected = await connection.ExecuteAsync(command);
        bool deleted = rowsAffected > 0;

        if (deleted)
        {
            _logger.LogInformation("Successfully deleted LAN device record for MAC {MacAddress}", macAddress);
        }
        else
        {
            _logger.LogWarning("Deletion failed. LAN device record for MAC {MacAddress} was not found", macAddress);
        }

        return deleted;
    }

    public async Task CleanOldDataAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default)
    {
        long cutoffEpochSeconds = cutoff.ToUnixTimeSeconds();
        const string sql = "DELETE FROM discovered_lan_devices WHERE last_seen < @Cutoff;";

        using IDbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        _logger.LogDebug("Deleting LAN device records last seen before epoch timestamp {Cutoff}", cutoffEpochSeconds);

        var command = new CommandDefinition(
            sql,
            new { Cutoff = cutoffEpochSeconds },
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        int rowsAffected = await connection.ExecuteAsync(command);

        if (rowsAffected > 0)
        {
            _logger.LogInformation("Successfully deleted {RowsAffected} outdated LAN device records", rowsAffected);
        }
        else
        {
            _logger.LogWarning("No LAN device records found prior to epoch timestamp {Cutoff}", cutoffEpochSeconds);
        }
    }
}
