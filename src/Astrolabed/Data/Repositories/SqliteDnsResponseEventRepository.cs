using System.Data;
using System.Net;

using Astrolabed;
using Astrolabed.Data.Entities;
using Astrolabed.Events;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Data.Repositories;

/// <summary>
/// SQLite implementation of IDnsResponseEventRepository utilizing Write-Ahead Logging (WAL) mode.
/// </summary>
public class SqliteDnsResponseEventRepository : IDnsResponseEventRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteDnsResponseEventRepository> _logger;

    public SqliteDnsResponseEventRepository(
        IOptions<ServerOptions> options,
        ILogger<SqliteDnsResponseEventRepository> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var config = options?.Value ?? throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(config.DbOptions.ConnectionString))
        {
            throw new ArgumentException("SQLite connection string cannot be empty.", nameof(options));
        }

        _connectionString = config.DbOptions.ConnectionString;

        DatabaseBuilder.InitializeDatabase(_connectionString);
    }

    private SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    public void Add(DnsResponseEvent responseEvent)
    {
        ArgumentNullException.ThrowIfNull(responseEvent);

        using var connection = CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = $"""
            INSERT INTO dns_response_events (Timestamp, ClientIp, ClientName, QueryName, QueryType, Status, ResponseIp)
            VALUES ($timestamp, $clientIp, $clientName, $queryName, $queryType, $status, $responseIp);
        """;

        command.Parameters.AddWithValue("$timestamp", responseEvent.Timestamp.ToString("o"));
        command.Parameters.AddWithValue("$clientIp", responseEvent.ClientIp.ToString());
        command.Parameters.AddWithValue("$clientName", (object?)responseEvent.ClientName ?? DBNull.Value);
        command.Parameters.AddWithValue("$queryName", responseEvent.QueryName);
        command.Parameters.AddWithValue("$queryType", responseEvent.QueryType);
        command.Parameters.AddWithValue("$status", responseEvent.Status);
        command.Parameters.AddWithValue("$responseIp", (object?)responseEvent.ResponseIp?.ToString() ?? DBNull.Value);

        try
        {
            command.ExecuteNonQuery();
            _logger.LogDebug("Saved DNS response event for domain {QueryName}", responseEvent.QueryName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing DNS response event for {QueryName} to SQLite", responseEvent.QueryName);
            throw;
        }
    }

    public async Task AddAsync(DnsResponseEvent responseEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(responseEvent);

        using var connection = CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = $"""
            INSERT INTO dns_response_events (Timestamp, ClientIp, ClientName, QueryName, QueryType, Status, ResponseIp)
            VALUES ($timestamp, $clientIp, $clientName, $queryName, $queryType, $status, $responseIp);
        """;

        command.Parameters.AddWithValue("$timestamp", responseEvent.Timestamp.ToString("o"));
        command.Parameters.AddWithValue("$clientIp", responseEvent.ClientIp.ToString());
        command.Parameters.AddWithValue("$clientName", (object?)responseEvent.ClientName ?? DBNull.Value);
        command.Parameters.AddWithValue("$queryName", responseEvent.QueryName);
        command.Parameters.AddWithValue("$queryType", responseEvent.QueryType);
        command.Parameters.AddWithValue("$status", responseEvent.Status);
        command.Parameters.AddWithValue("$responseIp", (object?)responseEvent.ResponseIp?.ToString() ?? DBNull.Value);

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
            _logger.LogDebug("Saved DNS response event asynchronously for domain {QueryName}", responseEvent.QueryName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing DNS response event asynchronously for {QueryName} to SQLite", responseEvent.QueryName);
            throw;
        }
    }

    public void AddBatch(IEnumerable<DnsResponseEvent> responseEvents)
    {
        ArgumentNullException.ThrowIfNull(responseEvents);

        using var connection = CreateConnection();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();

        command.Transaction = transaction;
        command.CommandText = $"""
            INSERT INTO dns_response_events (Timestamp, ClientIp, ClientName, QueryName, QueryType, Status, ResponseIp)
            VALUES ($timestamp, $clientIp, $clientName, $queryName, $queryType, $status, $responseIp);
        """;

        var pTimestamp = command.Parameters.Add("$timestamp", SqliteType.Text);
        var pClientIp = command.Parameters.Add("$clientIp", SqliteType.Text);
        var pClientName = command.Parameters.Add("$clientName", SqliteType.Text);
        var pQueryName = command.Parameters.Add("$queryName", SqliteType.Text);
        var pQueryType = command.Parameters.Add("$queryType", SqliteType.Text);
        var pStatus = command.Parameters.Add("$status", SqliteType.Text);
        var pResponseIp = command.Parameters.Add("$responseIp", SqliteType.Text);

        try
        {
            int count = 0;
            foreach (var responseEvent in responseEvents)
            {
                pTimestamp.Value = responseEvent.Timestamp.ToString("o");
                pClientIp.Value = responseEvent.ClientIp.ToString();
                pClientName.Value = (object?)responseEvent.ClientName ?? DBNull.Value;
                pQueryName.Value = responseEvent.QueryName;
                pQueryType.Value = responseEvent.QueryType;
                pStatus.Value = responseEvent.Status;
                pResponseIp.Value = (object?)responseEvent.ResponseIp?.ToString() ?? DBNull.Value;

                command.ExecuteNonQuery();
                count++;
            }

            transaction.Commit();
            _logger.LogInformation("Batch persisted {Count} DNS event records to SQLite", count);
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            _logger.LogError(ex, "Failed executing batch insert of DNS response events to SQLite");
            throw;
        }
    }

    public IEnumerable<DnsResponseEvent> GetByTimeRange(DateTimeOffset start, DateTimeOffset end, int limit = 100)
    {
        using var connection = CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = $"""
            SELECT Timestamp, ClientIp, ClientName, QueryName, QueryType, Status, ResponseIp
            FROM dns_response_events
            WHERE Timestamp >= $start AND Timestamp <= $end
            ORDER BY Timestamp DESC
            LIMIT $limit;
        """;

        command.Parameters.AddWithValue("$start", start.ToString("o"));
        command.Parameters.AddWithValue("$end", end.ToString("o"));
        command.Parameters.AddWithValue("$limit", limit);

        using var reader = command.ExecuteReader();
        var results = new List<DnsResponseEvent>();

        while (reader.Read())
        {
            results.Add(MapReaderToRecord(reader));
        }

        return results;
    }

    public IEnumerable<DnsResponseEvent> GetByClientIp(IPAddress clientIp, int limit = 100)
    {
        ArgumentNullException.ThrowIfNull(clientIp);

        using var connection = CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = $"""
            SELECT Timestamp, ClientIp, ClientName, QueryName, QueryType, Status, ResponseIp
            FROM dns_response_events
            WHERE ClientIp = $clientIp
            ORDER BY Timestamp DESC
            LIMIT $limit;
        """;

        command.Parameters.AddWithValue("$clientIp", clientIp.ToString());
        command.Parameters.AddWithValue("$limit", limit);

        using var reader = command.ExecuteReader();
        var results = new List<DnsResponseEvent>();

        while (reader.Read())
        {
            results.Add(MapReaderToRecord(reader));
        }

        return results;
    }

    public IEnumerable<DnsResponseEvent> GetByStatus(string status, int limit = 100)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);

        using var connection = CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = $"""
            SELECT Timestamp, ClientIp, ClientName, QueryName, QueryType, Status, ResponseIp
            FROM dns_response_events
            WHERE Status = $status
            ORDER BY Timestamp DESC
            LIMIT $limit;
        """;

        command.Parameters.AddWithValue("$status", status);
        command.Parameters.AddWithValue("$limit", limit);

        using var reader = command.ExecuteReader();
        var results = new List<DnsResponseEvent>();

        while (reader.Read())
        {
            results.Add(MapReaderToRecord(reader));
        }

        return results;
    }

    public IEnumerable<DnsResponseEvent> GetAll(int limit = 1000)
    {
        using var connection = CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = $"""
            SELECT Timestamp, ClientIp, ClientName, QueryName, QueryType, Status, ResponseIp
            FROM dns_response_events
            ORDER BY Timestamp DESC
            LIMIT $limit;
        """;

        command.Parameters.AddWithValue("$limit", limit);

        using var reader = command.ExecuteReader();
        var results = new List<DnsResponseEvent>();

        while (reader.Read())
        {
            results.Add(MapReaderToRecord(reader));
        }

        return results;
    }

    public int DeleteOlderThan(DateTimeOffset cutoff)
    {
        using var connection = CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = $"""
            DELETE FROM dns_response_events
            WHERE Timestamp < $cutoff;
        """;

        command.Parameters.AddWithValue("$cutoff", cutoff.ToString("o"));

        try
        {
            int deleted = command.ExecuteNonQuery();
            _logger.LogInformation("Purged {Count} DNS events older than {Cutoff} from SQLite", deleted, cutoff);
            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error purging DNS events older than {Cutoff} from SQLite", cutoff);
            throw;
        }
    }

    private static DnsResponseEvent MapReaderToRecord(SqliteDataReader reader)
    {
        var timestamp = DateTimeOffset.Parse(reader.GetString(0));
        var clientIp = IPAddress.Parse(reader.GetString(1));
        var clientName = reader.IsDBNull(2) ? null : reader.GetString(2);
        var queryName = reader.GetString(3);
        var queryType = reader.GetString(4);
        var status = reader.GetString(5);
        var responseIp = reader.IsDBNull(6) ? null : IPAddress.Parse(reader.GetString(6));

        return new DnsResponseEvent(
            Timestamp: timestamp,
            ClientIp: clientIp,
            ClientName: clientName,
            QueryName: queryName,
            QueryType: queryType,
            Status: status,
            ResponseIp: responseIp
        );
    }
}
