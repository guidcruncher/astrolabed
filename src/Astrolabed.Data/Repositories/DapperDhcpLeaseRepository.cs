using System.Data;
using System.Net;

using Astrolabed.Data.Models;
using Astrolabed.Data.Options;
using Astrolabed.Core.Network;

using Dapper;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Data.Repositories;

/// <summary>
/// Dapper implementation for managing <see cref="DhcpLease"/> persistence, 
/// mapping queries through <see cref="DhcpLeaseEntity"/> to maintain exact SQL representations
/// and enforcing colon-separated MAC address formatting.
/// </summary>
public sealed class DapperDhcpLeaseRepository : IDhcpLeaseRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly DatabaseOptions _databaseOptions;
    private readonly ILogger<DapperDhcpLeaseRepository> _logger;

    public DapperDhcpLeaseRepository(
        IDbConnectionFactory connectionFactory,
        IOptions<DatabaseOptions> databaseOptions,
        ILogger<DapperDhcpLeaseRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        ArgumentNullException.ThrowIfNull(databaseOptions);
        ArgumentNullException.ThrowIfNull(logger);

        _connectionFactory = connectionFactory;
        _databaseOptions = databaseOptions.Value;
        _logger = logger;
    }

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

        using IDbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        _logger.LogDebug("Fetching DHCP lease for ClientID {ClientId} or MAC {MacAddress}", clientId, formattedMac);

        var command = new CommandDefinition(
            sql,
            new { ClientId = clientId, MacAddress = formattedMac },
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        DhcpLeaseEntity? entity = await connection.QuerySingleOrDefaultAsync<DhcpLeaseEntity>(command);

        return entity?.ToDomain();
    }

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

        using IDbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        string ipString = ipAddress.ToString();
        _logger.LogDebug("Fetching active DHCP lease for IP {IpAddress}", ipString);

        var command = new CommandDefinition(
            sql,
            new { IpAddress = ipString },
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        DhcpLeaseEntity? entity = await connection.QuerySingleOrDefaultAsync<DhcpLeaseEntity>(command);

        return entity?.ToDomain();
    }

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

        using IDbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        string ipString = ipAddress.ToString();
        long nowEpochSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        _logger.LogDebug("Checking availability for IP {IpAddress} against ClientID {ClientId}", ipString, clientId);

        var command = new CommandDefinition(
            sql,
            new { IpAddress = ipString, ClientId = clientId, Now = nowEpochSeconds },
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        int count = await connection.ExecuteScalarAsync<int>(command);

        return count == 0;
    }

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
        DateTime now = DateTime.UtcNow;
        DateTime leaseEndTime = now.Add(duration);

        var lease = new DhcpLease
        {
            ClientId = clientId,
            ClientName = clientName,
            MacAddress = formattedMac,
            IpAddress = requestedIp,
            LeaseStartTime = now,
            LeaseEndTime = leaseEndTime,
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

        using IDbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        _logger.LogDebug("Allocating or updating lease for ClientID {ClientId}, MAC {MacAddress}, IP {IpAddress}", clientId, formattedMac, entity.IpAddress);

        var command = new CommandDefinition(
            sql,
            new
            {
                entity.ClientId,
                entity.ClientName,
                entity.MacAddress,
                entity.IpAddress,
                entity.LeaseStartTime,
                entity.LeaseEndTime,
                entity.IsActive
            },
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);

        _logger.LogInformation("Successfully allocated or updated lease for ClientID {ClientId} with IP {IpAddress}", clientId, entity.IpAddress);

        return lease;
    }

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

        using IDbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        _logger.LogDebug("Releasing lease for ClientID {ClientId} or MAC {MacAddress}", clientId, formattedMac);

        var command = new CommandDefinition(
            sql,
            new { ClientId = clientId, MacAddress = formattedMac },
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        int rowsAffected = await connection.ExecuteAsync(command);

        if (rowsAffected > 0)
        {
            _logger.LogInformation("Successfully released {RowsAffected} lease record(s) for ClientID {ClientId} / MAC {MacAddress}", rowsAffected, clientId, formattedMac);
        }
        else
        {
            _logger.LogWarning("No active lease found to release for ClientID {ClientId} / MAC {MacAddress}", clientId, formattedMac);
        }
    }
}

