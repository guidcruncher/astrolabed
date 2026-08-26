using System.Data.Common;
using System.Net;

using Astrolabed.Core.Network;
using Astrolabed.Data.Models;
using Astrolabed.Data.Options;
using Astrolabed.Data.Pagination;

using Dapper;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Data.Repositories;

/// <summary>
/// High-performance Dapper implementation for managing <see cref="DhcpLease"/> persistence.
/// Maps database queries through <see cref="DhcpLeaseEntity"/> to maintain exact SQL representations
/// and enforces standardized MAC address formatting.
/// </summary>
/// <remarks>
/// Targets .NET 10 features including primary constructors, asynchronous disposable database contexts,
/// structural parameter bindings to prevent GC allocations, and compile-time logger source generators.
/// </remarks>
/// <param name="connectionFactory">The asynchronous database connection factory.</param>
/// <param name="databaseOptions">Configuration options containing database operational settings.</param>
/// <param name="logger">Structured logger instance for diagnostic output.</param>
public sealed partial class DapperDhcpLeaseRepository(
    IDbConnectionFactory connectionFactory,
    IOptions<DatabaseOptions> databaseOptions,
    ILogger<DapperDhcpLeaseRepository> logger) : IDhcpLeaseRepository
{
    private readonly IDbConnectionFactory _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    private readonly DatabaseOptions _databaseOptions = databaseOptions?.Value ?? throw new ArgumentNullException(nameof(databaseOptions));
    private readonly ILogger<DapperDhcpLeaseRepository> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<DhcpLease?> GetLeaseByClientIdOrMacAsync(
        string clientId,
        string macAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(macAddress);

        string formattedMac = MacAddressFormatter.Format(macAddress);

        const string sql = """
            SELECT client_id AS ClientId,
                   client_name AS ClientName,
                   mac_address AS MacAddress,
                   ip_address AS IpAddress,
                   lease_start_time AS LeaseStartTime,
                   lease_end_time AS LeaseEndTime,
                   is_active AS IsActive
            FROM dhcp_leases
            WHERE client_id = @ClientId OR mac_address = @MacAddress
            ORDER BY is_active DESC, lease_end_time DESC
            LIMIT 1;
            """;

        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        LogFetchingLeaseByClientOrMac(_logger, clientId, formattedMac);

        var parameters = new DynamicParameters();
        parameters.Add("ClientId", clientId);
        parameters.Add("MacAddress", formattedMac);

        var command = new CommandDefinition(
            sql,
            parameters,
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        DhcpLeaseEntity? entity = await connection.QuerySingleOrDefaultAsync<DhcpLeaseEntity>(command);

        return entity?.ToDomain();
    }

    /// <inheritdoc />
    public async Task<DhcpLease?> GetLeaseByPtrAddressAsync(
        string ptrAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ptrAddress);

        var ipAddress = PtrConverter.ToIPAddress(ptrAddress);
        return await GetLeaseByIpAsync(ipAddress, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DhcpLease?> GetLeaseByIpAsync(
        IPAddress ipAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ipAddress);

        const string sql = """
            SELECT client_id AS ClientId,
                   client_name AS ClientName,
                   mac_address AS MacAddress,
                   ip_address AS IpAddress,
                   lease_start_time AS LeaseStartTime,
                   lease_end_time AS LeaseEndTime,
                   is_active AS IsActive
            FROM dhcp_leases
            WHERE ip_address = @IpAddress AND is_active = 1;
            """;

        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        string ipString = ipAddress.ToString();
        LogFetchingLeaseByIp(_logger, ipString);

        var parameters = new DynamicParameters();
        parameters.Add("IpAddress", ipString);

        var command = new CommandDefinition(
            sql,
            parameters,
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        DhcpLeaseEntity? entity = await connection.QuerySingleOrDefaultAsync<DhcpLeaseEntity>(command);

        return entity?.ToDomain();
    }

    /// <inheritdoc />
    public async Task<bool> IsIpAvailableAsync(
        IPAddress ipAddress,
        string clientId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ipAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

        const string sql = """
            SELECT COUNT(1)
            FROM dhcp_leases
            WHERE ip_address = @IpAddress 
              AND is_active = 1 
              AND client_id <> @ClientId
              AND lease_end_time > @Now;
            """;

        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        string ipString = ipAddress.ToString();
        long nowEpochSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        LogCheckingIpAvailability(_logger, ipString, clientId);

        var parameters = new DynamicParameters();
        parameters.Add("IpAddress", ipString);
        parameters.Add("ClientId", clientId);
        parameters.Add("Now", nowEpochSeconds);

        var command = new CommandDefinition(
            sql,
            parameters,
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        int count = await connection.ExecuteScalarAsync<int>(command);

        return count == 0;
    }

    /// <inheritdoc />
    public async Task<DhcpLease> AllocateOrUpdateLeaseAsync(
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
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset leaseEndTime = now.Add(duration);

        var lease = new DhcpLease
        {
            ClientId = clientId,
            ClientName = clientName,
            MacAddress = formattedMac,
            IpAddress = requestedIp,
            LeaseStartTime = now.UtcDateTime,
            LeaseEndTime = leaseEndTime.UtcDateTime,
            IsActive = true
        };

        DhcpLeaseEntity entity = DhcpLeaseEntity.FromDomain(lease);

        const string sql = """
            INSERT INTO dhcp_leases (
                client_id, client_name, mac_address, ip_address, lease_start_time, lease_end_time, is_active
            ) VALUES (
                @ClientId, @ClientName, @MacAddress, @IpAddress, @LeaseStartTime, @LeaseEndTime, @IsActive
            )
            ON CONFLICT (client_id) DO UPDATE SET
                client_name = EXCLUDED.client_name,
                mac_address = EXCLUDED.mac_address,
                ip_address = EXCLUDED.ip_address,
                lease_start_time = EXCLUDED.lease_start_time,
                lease_end_time = EXCLUDED.lease_end_time,
                is_active = EXCLUDED.is_active;
            """;

        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        LogAllocatingLease(_logger, clientId, formattedMac, entity.IpAddress);

        var parameters = new DynamicParameters();
        parameters.Add("ClientId", entity.ClientId);
        parameters.Add("ClientName", entity.ClientName);
        parameters.Add("MacAddress", entity.MacAddress);
        parameters.Add("IpAddress", entity.IpAddress);
        parameters.Add("LeaseStartTime", entity.LeaseStartTime);
        parameters.Add("LeaseEndTime", entity.LeaseEndTime);
        parameters.Add("IsActive", entity.IsActive);

        var command = new CommandDefinition(
            sql,
            parameters,
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);

        LogLeaseAllocatedSuccessfully(_logger, clientId, entity.IpAddress);

        return lease;
    }

    /// <inheritdoc />
    public async Task ReleaseLeaseAsync(
        string clientId,
        string macAddress,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(macAddress);

        string formattedMac = MacAddressFormatter.Format(macAddress);

        const string sql = """
            UPDATE dhcp_leases
            SET is_active = 0
            WHERE client_id = @ClientId OR mac_address = @MacAddress;
            """;

        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        LogReleasingLease(_logger, clientId, formattedMac);

        var parameters = new DynamicParameters();
        parameters.Add("ClientId", clientId);
        parameters.Add("MacAddress", formattedMac);

        var command = new CommandDefinition(
            sql,
            parameters,
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        int rowsAffected = await connection.ExecuteAsync(command);

        if (rowsAffected > 0)
        {
            LogLeaseReleasedSuccessfully(_logger, rowsAffected, clientId, formattedMac);
        }
        else
        {
            LogNoActiveLeaseFoundToRelease(_logger, clientId, formattedMac);
        }
    }

    /// <inheritdoc />
    public async Task<PagedResult<DhcpLease>> GetLeasesAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        int offset = (pageNumber - 1) * pageSize;
        const string sql = """
            SELECT COUNT(1)
            FROM dhcp_leases;
            SELECT client_id AS ClientId,
                   client_name AS ClientName,
                   mac_address AS MacAddress,
                   ip_address AS IpAddress,
                   lease_start_time AS LeaseStartTime,
                   lease_end_time AS LeaseEndTime,
                   is_active AS IsActive
            FROM dhcp_leases
            ORDER BY lease_end_time DESC
            LIMIT @PageSize OFFSET @Offset;
            """;
        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var parameters = new DynamicParameters();
        parameters.Add("PageSize", pageSize);
        parameters.Add("Offset", offset);
        var command = new CommandDefinition(
            sql,
            parameters,
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);
        await using SqlMapper.GridReader gridReader = await connection.QueryMultipleAsync(command);
        int totalCount = await gridReader.ReadSingleAsync<int>();
        IEnumerable<DhcpLeaseEntity> entities = await gridReader.ReadAsync<DhcpLeaseEntity>();
        IReadOnlyCollection<DhcpLease> leases = entities.Select(entity => entity.ToDomain()).ToList().AsReadOnly();
        return leases.ToPagedResult<DhcpLease>(pageNumber, pageSize);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Fetching DHCP lease for ClientID {ClientId} or MAC {MacAddress}")]
    private static partial void LogFetchingLeaseByClientOrMac(ILogger logger, string clientId, string macAddress);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "Fetching active DHCP lease for IP {IpAddress}")]
    private static partial void LogFetchingLeaseByIp(ILogger logger, string ipAddress);

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "Checking availability for IP {IpAddress} against ClientID {ClientId}")]
    private static partial void LogCheckingIpAvailability(ILogger logger, string ipAddress, string clientId);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Allocating or updating lease for ClientID {ClientId}, MAC {MacAddress}, IP {IpAddress}")]
    private static partial void LogAllocatingLease(ILogger logger, string clientId, string macAddress, string? ipAddress);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "Successfully allocated or updated lease for ClientID {ClientId} with IP {IpAddress}")]
    private static partial void LogLeaseAllocatedSuccessfully(ILogger logger, string clientId, string? ipAddress);

    [LoggerMessage(EventId = 6, Level = LogLevel.Debug, Message = "Releasing lease for ClientID {ClientId} or MAC {MacAddress}")]
    private static partial void LogReleasingLease(ILogger logger, string clientId, string macAddress);

    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "Successfully released {RowsAffected} lease record(s) for ClientID {ClientId} / MAC {MacAddress}")]
    private static partial void LogLeaseReleasedSuccessfully(ILogger logger, int rowsAffected, string clientId, string macAddress);

    [LoggerMessage(EventId = 8, Level = LogLevel.Warning, Message = "No active lease found to release for ClientID {ClientId} / MAC {MacAddress}")]
    private static partial void LogNoActiveLeaseFoundToRelease(ILogger logger, string clientId, string macAddress);
}
