using System.Data.Common;
using System.Net;

using Astrolabed.Data.Models;
using Astrolabed.Data.Options;
using Astrolabed.Data.Pagination;

using Dapper;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Data.Repositories;

/// <summary>
/// High-performance Dapper implementation for managing <see cref="DiscoveredLanDevice"/> persistence,
/// mapping database queries through <see cref="DiscoveredLanDeviceEntity"/> to maintain exact SQL representations.
/// </summary>
/// <remarks>
/// Enforces .NET 10 asynchronous execution via <see cref="DbConnection"/>, allocation-optimized batch execution,
/// primary constructor usage, and source-generated structured logging.
/// </remarks>
/// <param name="connectionFactory">The database connection factory providing asynchronous database access.</param>
/// <param name="databaseOptions">Database configuration settings, including command execution timeouts.</param>
/// <param name="logger">Structured logging instance for diagnostic and operational logs.</param>
public sealed partial class DapperDiscoveredLanDeviceRepository(
    IDbConnectionFactory connectionFactory,
    IOptions<DatabaseOptions> databaseOptions,
    ILogger<DapperDiscoveredLanDeviceRepository> logger) : IDiscoveredLanDeviceRepository
{
    private readonly IDbConnectionFactory _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    private readonly DatabaseOptions _databaseOptions = databaseOptions?.Value ?? throw new ArgumentNullException(nameof(databaseOptions));
    private readonly ILogger<DapperDiscoveredLanDeviceRepository> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task UpsertAsync(DiscoveredLanDevice device, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);

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

        long firstSeenEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        DiscoveredLanDeviceEntity entity = DiscoveredLanDeviceEntity.FromDomain(device);

        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        LogUpsertingLanDevice(_logger, device.MacAddress);

        var parameters = new DynamicParameters();
        parameters.Add("MacAddress", entity.MacAddress);
        parameters.Add("IpAddress", entity.IpAddress);
        parameters.Add("PtrAddress", entity.PtrAddress);
        parameters.Add("HostName", entity.HostName);
        parameters.Add("LastSeen", entity.LastSeen);
        parameters.Add("FirstSeen", firstSeenEpoch);

        var command = new CommandDefinition(
            sql,
            parameters,
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);

        LogUpsertedLanDeviceSuccessfully(_logger, device.MacAddress);
    }

    /// <inheritdoc />
    public async Task BulkUpsertAsync(IEnumerable<DiscoveredLanDevice> devices, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(devices);

        ReadOnlySpan<DiscoveredLanDevice> deviceSpan = devices switch
        {
            DiscoveredLanDevice[] array => array,
            List<DiscoveredLanDevice> list => list.ToArray(),
            _ => devices.ToArray()
        };

        if (deviceSpan.IsEmpty)
        {
            LogBulkUpsertSkippedEmpty(_logger);
            return;
        }

        long firstSeenEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var parameterBatch = new DynamicParameters[deviceSpan.Length];
        for (int i = 0; i < deviceSpan.Length; i++)
        {
            DiscoveredLanDeviceEntity entity = DiscoveredLanDeviceEntity.FromDomain(deviceSpan[i]);
            var param = new DynamicParameters();
            param.Add("MacAddress", entity.MacAddress);
            param.Add("IpAddress", entity.IpAddress);
            param.Add("PtrAddress", entity.PtrAddress);
            param.Add("HostName", entity.HostName);
            param.Add("LastSeen", entity.LastSeen);
            param.Add("FirstSeen", firstSeenEpoch);
            parameterBatch[i] = param;
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

        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        LogExecutingBulkUpsert(_logger, deviceSpan.Length);

        var command = new CommandDefinition(
            sql,
            parameterBatch,
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        int rowsAffected = await connection.ExecuteAsync(command);

        LogBulkUpsertCompletedSuccessfully(_logger, deviceSpan.Length, rowsAffected);
    }

    /// <inheritdoc />
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

        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        LogFetchingByPtrAddress(_logger, ptrAddress);

        var parameters = new DynamicParameters();
        parameters.Add("PtrAddress", ptrAddress);

        var command = new CommandDefinition(
            sql,
            parameters,
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        DiscoveredLanDeviceEntity? entity = await connection.QuerySingleOrDefaultAsync<DiscoveredLanDeviceEntity>(command);

        return entity?.ToDomain();
    }

    /// <inheritdoc />
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

        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        LogFetchingByMacAddress(_logger, macAddress);

        var parameters = new DynamicParameters();
        parameters.Add("MacAddress", macAddress);

        var command = new CommandDefinition(
            sql,
            parameters,
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        DiscoveredLanDeviceEntity? entity = await connection.QuerySingleOrDefaultAsync<DiscoveredLanDeviceEntity>(command);

        return entity?.ToDomain();
    }

    /// <inheritdoc />
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

        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        string ipString = ipAddress.ToString();
        LogFetchingByIpAddress(_logger, ipString);

        var parameters = new DynamicParameters();
        parameters.Add("IpAddress", ipString);

        var command = new CommandDefinition(
            sql,
            parameters,
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        DiscoveredLanDeviceEntity? entity = await connection.QuerySingleOrDefaultAsync<DiscoveredLanDeviceEntity>(command);

        return entity?.ToDomain();
    }

    /// <inheritdoc />
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

        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        LogExecutingPagedSelect(_logger, targetPage, targetSize);

        var parameters = new DynamicParameters();
        parameters.Add("PageSize", targetSize);
        parameters.Add("Offset", offset);

        var command = new CommandDefinition(
            sql,
            parameters,
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        await using SqlMapper.GridReader gridReader = await connection.QueryMultipleAsync(command);

        long totalCount = await gridReader.ReadSingleAsync<long>();
        IEnumerable<DiscoveredLanDeviceEntity> entities = await gridReader.ReadAsync<DiscoveredLanDeviceEntity>();

        List<DiscoveredLanDevice> items = entities.Select(e => e.ToDomain()).ToList();

        LogRetrievedPagedResults(_logger, targetPage, items.Count, totalCount);

        return PagedResult<DiscoveredLanDevice>.Create(items, totalCount, targetPage, targetSize);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteByMacAddressAsync(string macAddress, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(macAddress);

        const string sql = "DELETE FROM discovered_lan_devices WHERE mac_address = @MacAddress;";

        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        LogDeletingByMacAddress(_logger, macAddress);

        var parameters = new DynamicParameters();
        parameters.Add("MacAddress", macAddress);

        var command = new CommandDefinition(
            sql,
            parameters,
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        int rowsAffected = await connection.ExecuteAsync(command);
        bool deleted = rowsAffected > 0;

        if (deleted)
        {
            LogDeletedByMacAddressSuccessfully(_logger, macAddress);
        }
        else
        {
            LogDeleteByMacAddressFailedNotFound(_logger, macAddress);
        }

        return deleted;
    }

    /// <inheritdoc />
    public async Task CleanOldDataAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default)
    {
        long cutoffEpochSeconds = cutoff.ToUnixTimeSeconds();
        const string sql = "DELETE FROM discovered_lan_devices WHERE last_seen < @Cutoff;";

        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        LogCleaningOldData(_logger, cutoffEpochSeconds);

        var parameters = new DynamicParameters();
        parameters.Add("Cutoff", cutoffEpochSeconds);

        var command = new CommandDefinition(
            sql,
            parameters,
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        int rowsAffected = await connection.ExecuteAsync(command);

        if (rowsAffected > 0)
        {
            LogCleanedOldDataSuccessfully(_logger, rowsAffected);
        }
        else
        {
            LogNoOldDataFoundToClean(_logger, cutoffEpochSeconds);
        }
    }

    [LoggerMessage(EventId = 101, Level = LogLevel.Debug, Message = "Upserting LAN device record for MAC {MacAddress}")]
    private static partial void LogUpsertingLanDevice(ILogger logger, string? macAddress);

    [LoggerMessage(EventId = 102, Level = LogLevel.Information, Message = "Successfully upserted LAN device record for MAC {MacAddress}")]
    private static partial void LogUpsertedLanDeviceSuccessfully(ILogger logger, string? macAddress);

    [LoggerMessage(EventId = 103, Level = LogLevel.Debug, Message = "Bulk upsert skipped as the provided collection contains no items")]
    private static partial void LogBulkUpsertSkippedEmpty(ILogger logger);

    [LoggerMessage(EventId = 104, Level = LogLevel.Debug, Message = "Executing bulk upsert for {Count} LAN device records")]
    private static partial void LogExecutingBulkUpsert(ILogger logger, int count);

    [LoggerMessage(EventId = 105, Level = LogLevel.Information, Message = "Successfully completed bulk upsert for {RequestedCount} devices ({RowsAffected} rows modified)")]
    private static partial void LogBulkUpsertCompletedSuccessfully(ILogger logger, int requestedCount, int rowsAffected);

    [LoggerMessage(EventId = 106, Level = LogLevel.Debug, Message = "Fetching LAN device record for PTR {PtrAddress}")]
    private static partial void LogFetchingByPtrAddress(ILogger logger, string ptrAddress);

    [LoggerMessage(EventId = 107, Level = LogLevel.Debug, Message = "Fetching LAN device record for MAC {MacAddress}")]
    private static partial void LogFetchingByMacAddress(ILogger logger, string macAddress);

    [LoggerMessage(EventId = 108, Level = LogLevel.Debug, Message = "Fetching LAN device record for IP {IpAddress}")]
    private static partial void LogFetchingByIpAddress(ILogger logger, string ipAddress);

    [LoggerMessage(EventId = 109, Level = LogLevel.Debug, Message = "Executing paged SELECT for LAN devices. PageNumber: {PageNumber}, PageSize: {PageSize}")]
    private static partial void LogExecutingPagedSelect(ILogger logger, int pageNumber, int pageSize);

    [LoggerMessage(EventId = 110, Level = LogLevel.Information, Message = "Retrieved page {PageNumber} with {Count} LAN device records (Total dataset size: {TotalCount})")]
    private static partial void LogRetrievedPagedResults(ILogger logger, int pageNumber, int count, long totalCount);

    [LoggerMessage(EventId = 111, Level = LogLevel.Debug, Message = "Deleting LAN device record for MAC {MacAddress}")]
    private static partial void LogDeletingByMacAddress(ILogger logger, string macAddress);

    [LoggerMessage(EventId = 112, Level = LogLevel.Information, Message = "Successfully deleted LAN device record for MAC {MacAddress}")]
    private static partial void LogDeletedByMacAddressSuccessfully(ILogger logger, string macAddress);

    [LoggerMessage(EventId = 113, Level = LogLevel.Warning, Message = "Deletion failed. LAN device record for MAC {MacAddress} was not found")]
    private static partial void LogDeleteByMacAddressFailedNotFound(ILogger logger, string macAddress);

    [LoggerMessage(EventId = 114, Level = LogLevel.Debug, Message = "Deleting LAN device records last seen before epoch timestamp {Cutoff}")]
    private static partial void LogCleaningOldData(ILogger logger, long cutoff);

    [LoggerMessage(EventId = 115, Level = LogLevel.Information, Message = "Successfully deleted {RowsAffected} outdated LAN device records")]
    private static partial void LogCleanedOldDataSuccessfully(ILogger logger, int rowsAffected);

    [LoggerMessage(EventId = 116, Level = LogLevel.Warning, Message = "No LAN device records found prior to epoch timestamp {Cutoff}")]
    private static partial void LogNoOldDataFoundToClean(ILogger logger, long cutoff);
}

