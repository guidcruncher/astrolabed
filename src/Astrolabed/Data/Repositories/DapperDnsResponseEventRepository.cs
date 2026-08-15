using System.Data;
using System.Net;

using Astrolabed;
using Astrolabed.Data.Entities;
using Astrolabed.Data.Repositories;
using Astrolabed.Events;

using Dapper;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Npgsql;

namespace Astrolabed.Data.Repositories;

/// <summary>
/// Provider‑agnostic Dapper implementation of IDnsResponseEventRepository.
/// Supports SQLite and PostgreSQL via configuration.
/// </summary>
public class DapperDnsResponseEventRepository : IDnsResponseEventRepository
{
    private readonly IDbConnectionFactory _factory;
    private readonly ILogger<DapperDnsResponseEventRepository> _logger;

    public DapperDnsResponseEventRepository(
        IDbConnectionFactory factory,
        ILogger<DapperDnsResponseEventRepository> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    // ----------------------------------------------------------------------
    // INSERT (sync)
    // ----------------------------------------------------------------------
    public void Add(DnsResponseEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);

        using var conn = _factory.Create();

        const string sql = """
            INSERT INTO dns_response_events
            (Timestamp, ClientIp, ClientName, QueryName, QueryType, Status, ResponseIp)
            VALUES (@Timestamp, @ClientIp, @ClientName, @QueryName, @QueryType, @Status, @ResponseIp)
        """;

        conn.Execute(sql, new
        {
            Timestamp = e.Timestamp.ToString("o"),
            ClientIp = e.ClientIp.ToString(),
            ClientName = e.ClientName,
            QueryName = e.QueryName,
            QueryType = e.QueryType,
            Status = e.Status,
            ResponseIp = e.ResponseIp?.ToString()
        });

        _logger.LogDebug("Saved DNS response event for domain {QueryName}", e.QueryName);
    }

    // ----------------------------------------------------------------------
    // INSERT (async)
    // ----------------------------------------------------------------------
    public async Task AddAsync(DnsResponseEvent e, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(e);

        using var conn = _factory.Create();

        const string sql = """
            INSERT INTO dns_response_events
            (Timestamp, ClientIp, ClientName, QueryName, QueryType, Status, ResponseIp)
            VALUES (@Timestamp, @ClientIp, @ClientName, @QueryName, @QueryType, @Status, @ResponseIp)
        """;

        await conn.ExecuteAsync(sql, new
        {
            Timestamp = e.Timestamp.ToString("o"),
            ClientIp = e.ClientIp.ToString(),
            ClientName = e.ClientName,
            QueryName = e.QueryName,
            QueryType = e.QueryType,
            Status = e.Status,
            ResponseIp = e.ResponseIp?.ToString()
        });

        _logger.LogDebug("Saved DNS response event asynchronously for domain {QueryName}", e.QueryName);
    }

    // ----------------------------------------------------------------------
    // BATCH INSERT
    // ----------------------------------------------------------------------
    public void AddBatch(IEnumerable<DnsResponseEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        using var conn = _factory.Create();
        using var tx = conn.BeginTransaction();

        const string sql = """
            INSERT INTO dns_response_events
            (Timestamp, ClientIp, ClientName, QueryName, QueryType, Status, ResponseIp)
            VALUES (@Timestamp, @ClientIp, @ClientName, @QueryName, @QueryType, @Status, @ResponseIp)
        """;

        int count = 0;

        foreach (var e in events)
        {
            conn.Execute(sql, new
            {
                Timestamp = e.Timestamp.ToString("o"),
                ClientIp = e.ClientIp.ToString(),
                ClientName = e.ClientName,
                QueryName = e.QueryName,
                QueryType = e.QueryType,
                Status = e.Status,
                ResponseIp = e.ResponseIp?.ToString()
            }, tx);

            count++;
        }

        tx.Commit();
        _logger.LogInformation("Batch persisted {Count} DNS event records", count);
    }

    // ----------------------------------------------------------------------
    // QUERIES
    // ----------------------------------------------------------------------
    public IEnumerable<DnsResponseEvent> GetByTimeRange(DateTimeOffset start, DateTimeOffset end, int limit = 100)
    {
        using var conn = _factory.Create();

        const string sql = """
            SELECT Timestamp, ClientIp, ClientName, QueryName, QueryType, Status, ResponseIp
            FROM dns_response_events
            WHERE Timestamp >= @Start AND Timestamp <= @End
            ORDER BY Timestamp DESC
            LIMIT @Limit
        """;

        var rows = conn.Query<DnsResponseEventRaw>(sql, new
        {
            Start = start.ToString("o"),
            End = end.ToString("o"),
            Limit = limit
        });

        return rows.Select(Map);
    }

    public IEnumerable<DnsResponseEvent> GetByClientIp(IPAddress clientIp, int limit = 100)
    {
        using var conn = _factory.Create();

        const string sql = """
            SELECT Timestamp, ClientIp, ClientName, QueryName, QueryType, Status, ResponseIp
            FROM dns_response_events
            WHERE ClientIp = @ClientIp
            ORDER BY Timestamp DESC
            LIMIT @Limit
        """;

        var rows = conn.Query<DnsResponseEventRaw>(sql, new
        {
            ClientIp = clientIp.ToString(),
            Limit = limit
        });

        return rows.Select(Map);
    }

    public IEnumerable<DnsResponseEvent> GetByStatus(string status, int limit = 100)
    {
        using var conn = _factory.Create();

        const string sql = """
            SELECT Timestamp, ClientIp, ClientName, QueryName, QueryType, Status, ResponseIp
            FROM dns_response_events
            WHERE Status = @Status
            ORDER BY Timestamp DESC
            LIMIT @Limit
        """;

        var rows = conn.Query<DnsResponseEventRaw>(sql, new
        {
            Status = status,
            Limit = limit
        });

        return rows.Select(Map);
    }

    public IEnumerable<DnsResponseEvent> GetAll(int limit = 1000)
    {
        using var conn = _factory.Create();

        const string sql = """
            SELECT Timestamp, ClientIp, ClientName, QueryName, QueryType, Status, ResponseIp
            FROM dns_response_events
            ORDER BY Timestamp DESC
            LIMIT @Limit
        """;

        var rows = conn.Query<DnsResponseEventRaw>(sql, new { Limit = limit });

        return rows.Select(Map);
    }

    // ----------------------------------------------------------------------
    // DELETE
    // ----------------------------------------------------------------------
    public int DeleteOlderThan(DateTimeOffset cutoff)
    {
        using var conn = _factory.Create();

        const string sql = """
            DELETE FROM dns_response_events
            WHERE Timestamp < @Cutoff
        """;

        int deleted = conn.Execute(sql, new { Cutoff = cutoff.ToString("o") });

        _logger.LogInformation("Purged {Count} DNS events older than {Cutoff}", deleted, cutoff);
        return deleted;
    }

    // ----------------------------------------------------------------------
    // MAPPING
    // ----------------------------------------------------------------------
    private static DnsResponseEvent Map(DnsResponseEventRaw r)
    {
        return new DnsResponseEvent(
            Timestamp: DateTimeOffset.Parse(r.Timestamp),
            ClientIp: IPAddress.Parse(r.ClientIp),
            ClientName: r.ClientName,
            QueryName: r.QueryName,
            QueryType: r.QueryType,
            Status: r.Status,
            ResponseIp: r.ResponseIp is null ? null : IPAddress.Parse(r.ResponseIp)
        );
    }

    private record DnsResponseEventRaw(
        string Timestamp,
        string ClientIp,
        string? ClientName,
        string QueryName,
        string QueryType,
        string Status,
        string? ResponseIp
    );
}
