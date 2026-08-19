using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Data.Entities;
using Astrolabed.Events;

using Dapper;

using Microsoft.Extensions.Logging;

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
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(logger);

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
            (Timestamp, ClientIp, ClientName, QueryName, QueryType, Status, ResponseIp, TimestampEpoch, IsBlocked)
            VALUES (@Timestamp, @ClientIp, @ClientName, @QueryName, @QueryType, @Status, @ResponseIp, @TimestampEpoch, @IsBlocked)
        """;

        conn.Execute(sql, new
        {
            TimestampEpoch = e.TimestampEpoch,
            IsBlocked = e.IsBlocked ? 1 : 0,
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
            (Timestamp, ClientIp, ClientName, QueryName, QueryType, Status, ResponseIp, TimestampEpoch, IsBlocked)
            VALUES (@Timestamp, @ClientIp, @ClientName, @QueryName, @QueryType, @Status, @ResponseIp, @TimestampEpoch, @IsBlocked)
        """;

        await conn.ExecuteAsync(sql, new
        {
            TimestampEpoch = e.TimestampEpoch,
            IsBlocked = e.IsBlocked ? 1 : 0,
            Timestamp = e.Timestamp.ToString("o"),
            ClientIp = e.ClientIp.ToString(),
            ClientName = e.ClientName,
            QueryName = e.QueryName,
            QueryType = e.QueryType,
            Status = e.Status,
            ResponseIp = e.ResponseIp?.ToString()
        }).ConfigureAwait(false);

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
            (Timestamp, ClientIp, ClientName, QueryName, QueryType, Status, ResponseIp, TimestampEpoch, IsBlocked)
            VALUES (@Timestamp, @ClientIp, @ClientName, @QueryName, @QueryType, @Status, @ResponseIp, @TimestampEpoch, @IsBlocked)
        """;

        int count = 0;

        foreach (var e in events)
        {
            conn.Execute(sql, new
            {
                TimestampEpoch = e.TimestampEpoch,
                IsBlocked = e.IsBlocked ? 1 : 0,
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
    public PagedResult<DnsResponseEvent> GetByTimeRange(
        DateTimeOffset start,
        DateTimeOffset end,
        int pageNumber = 1,
        int pageSize = 100)
    {
        NormalizePagination(ref pageNumber, ref pageSize);

        using var conn = _factory.Create();

        const string sql = """
            SELECT COUNT(1) FROM dns_response_events WHERE Timestamp >= @Start AND Timestamp <= @End;

            SELECT Timestamp, ClientIp, ClientName, QueryName, QueryType, Status, ResponseIp, TimestampEpoch, IsBlocked
            FROM dns_response_events
            WHERE Timestamp >= @Start AND Timestamp <= @End
            ORDER BY Timestamp DESC
            LIMIT @Limit OFFSET @Offset;
        """;

        using var multi = conn.QueryMultiple(sql, new
        {
            Start = start.ToString("o"),
            End = end.ToString("o"),
            Limit = pageSize,
            Offset = (pageNumber - 1) * pageSize
        });

        int totalCount = multi.ReadFirst<int>();
        var items = multi.Read<DnsResponseEventRaw>().Select(Map).ToList();

        return new PagedResult<DnsResponseEvent>(items, totalCount, pageNumber, pageSize);
    }

    public PagedResult<DnsResponseEvent> GetByClientIp(
        IPAddress clientIp,
        int pageNumber = 1,
        int pageSize = 100)
    {
        ArgumentNullException.ThrowIfNull(clientIp);
        NormalizePagination(ref pageNumber, ref pageSize);

        using var conn = _factory.Create();

        const string sql = """
            SELECT COUNT(1) FROM dns_response_events WHERE ClientIp = @ClientIp;

            SELECT Timestamp, ClientIp, ClientName, QueryName, QueryType, Status, ResponseIp, TimestampEpoch, IsBlocked
            FROM dns_response_events
            WHERE ClientIp = @ClientIp
            ORDER BY Timestamp DESC
            LIMIT @Limit OFFSET @Offset;
        """;

        using var multi = conn.QueryMultiple(sql, new
        {
            ClientIp = clientIp.ToString(),
            Limit = pageSize,
            Offset = (pageNumber - 1) * pageSize
        });

        int totalCount = multi.ReadFirst<int>();
        var items = multi.Read<DnsResponseEventRaw>().Select(Map).ToList();

        return new PagedResult<DnsResponseEvent>(items, totalCount, pageNumber, pageSize);
    }

    public PagedResult<DnsResponseEvent> GetByStatus(
        string status,
        int pageNumber = 1,
        int pageSize = 100)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);
        NormalizePagination(ref pageNumber, ref pageSize);

        using var conn = _factory.Create();

        const string sql = """
            SELECT COUNT(1) FROM dns_response_events WHERE Status = @Status;

            SELECT Timestamp, ClientIp, ClientName, QueryName, QueryType, Status, ResponseIp, TimestampEpoch, IsBlocked
            FROM dns_response_events
            WHERE Status = @Status
            ORDER BY Timestamp DESC
            LIMIT @Limit OFFSET @Offset;
        """;

        using var multi = conn.QueryMultiple(sql, new
        {
            Status = status,
            Limit = pageSize,
            Offset = (pageNumber - 1) * pageSize
        });

        int totalCount = multi.ReadFirst<int>();
        var items = multi.Read<DnsResponseEventRaw>().Select(Map).ToList();

        return new PagedResult<DnsResponseEvent>(items, totalCount, pageNumber, pageSize);
    }

    public PagedResult<DnsResponseEvent> GetAll(
        int pageNumber = 1,
        int pageSize = 100)
    {
        NormalizePagination(ref pageNumber, ref pageSize);

        using var conn = _factory.Create();

        const string sql = """
            SELECT COUNT(1) FROM dns_response_events;

            SELECT Timestamp, ClientIp, ClientName, QueryName, QueryType, Status, ResponseIp, TimestampEpoch, IsBlocked
            FROM dns_response_events
            ORDER BY Timestamp DESC
            LIMIT @Limit OFFSET @Offset;
        """;

        using var multi = conn.QueryMultiple(sql, new
        {
            Limit = pageSize,
            Offset = (pageNumber - 1) * pageSize
        });

        int totalCount = multi.ReadFirst<int>();
        var items = multi.Read<DnsResponseEventRaw>().Select(Map).ToList();

        return new PagedResult<DnsResponseEvent>(items, totalCount, pageNumber, pageSize);
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
    // HELPERS & MAPPING
    // ----------------------------------------------------------------------
    private static void NormalizePagination(ref int pageNumber, ref int pageSize)
    {
        if (pageNumber < 1)
        {
            pageNumber = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 10;
        }
        else if (pageSize > 1000)
        {
            pageSize = 1000;
        }
    }

    private static DnsResponseEvent Map(DnsResponseEventRaw r)
    {
        return new DnsResponseEvent(
            TimestampEpoch: r.TimestampEpoch,
            IsBlocked: r.IsBlocked == 1,
            Timestamp: DateTimeOffset.Parse(r.Timestamp),
            ClientIp: IPAddress.Parse(r.ClientIp),
            ClientName: r.ClientName,
            QueryName: r.QueryName,
            QueryType: r.QueryType,
            Status: r.Status,
            ResponseIp: r.ResponseIp is null ? null : IPAddress.Parse(r.ResponseIp)
        );
    }

    private class DnsResponseEventRaw
    {
        public string Timestamp { get; set; } = string.Empty;
        public string ClientIp { get; set; } = string.Empty;
        public string? ClientName { get; set; }
        public string QueryName { get; set; } = string.Empty;
        public string QueryType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ResponseIp { get; set; }
        public long TimestampEpoch { get; set; }
        public long IsBlocked { get; set; }
    }
}
