using System.Data;

using Astrolabed.Data.Models;

using Astrolabed.Data.Options;
using Astrolabed.Data.Pagination;

using Dapper;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Data.Repositories;

/// <summary>
/// Dapper implementation supporting cross-database (PostgreSQL and SQLite) parameterized queries.
/// </summary>
public sealed class DapperDnsResponseEventRepository : IDnsResponseEventRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly DatabaseOptions _databaseOptions;
    private readonly ILogger<DapperDnsResponseEventRepository> _logger;

    public DapperDnsResponseEventRepository(
        IDbConnectionFactory connectionFactory,
        IOptions<DatabaseOptions> databaseOptions,
        ILogger<DapperDnsResponseEventRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        ArgumentNullException.ThrowIfNull(databaseOptions);
        ArgumentNullException.ThrowIfNull(logger);

        _connectionFactory = connectionFactory;
        _databaseOptions = databaseOptions.Value;
        _logger = logger;
    }

    public async Task AddAsync(DnsResponseEventEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        const string sql = """
            INSERT INTO dns_response_events (
                id, start_time_utc, context_id, question_name, question_type,
                client_endpoint, client_name, resolution_source, duration_ms
            ) VALUES (
                @Id, @StartTimeUtc, @ContextId, @QuestionName, @QuestionType,
                @ClientEndpoint, @ClientName, @ResolutionSource, @DurationMs
            );
            """;

        using IDbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        _logger.LogDebug("Inserting DNS response event record {Id}", entity.Id);

        var command = new CommandDefinition(
            sql,
            entity,
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);

        _logger.LogInformation("Successfully inserted DNS response event record {Id}", entity.Id);
    }

    public async Task<DnsResponseEventEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        const string sql = """
            SELECT id, start_time_utc, context_id, question_name, question_type,
                   client_endpoint, client_name, resolution_source, duration_ms
            FROM dns_response_events
            WHERE id = @Id;
            """;

        using IDbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        _logger.LogDebug("Fetching DNS response event record by ID {Id}", id);

        var command = new CommandDefinition(
            sql,
            new { Id = id },
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<DnsResponseEventEntity>(command);
    }

    public async Task<PagedResult<DnsResponseEventEntity>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        int targetPage = pageNumber < 1 ? 1 : pageNumber;
        int targetSize = pageSize < 1
            ? 10
            : Math.Min(pageSize, 100);

        int offset = (targetPage - 1) * targetSize;

        const string sql = """
            SELECT COUNT(1) FROM dns_response_events;

            SELECT id, start_time_utc, context_id, question_name, question_type,
                   client_endpoint, client_name, resolution_source, duration_ms
            FROM dns_response_events
            ORDER BY start_time_utc DESC
            LIMIT @PageSize OFFSET @Offset;
            """;

        using IDbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        _logger.LogDebug(
            "Executing paged SELECT query. PageNumber: {PageNumber}, PageSize: {PageSize}",
            targetPage,
            targetSize);

        var command = new CommandDefinition(
            sql,
            new { PageSize = targetSize, Offset = offset },
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        using SqlMapper.GridReader gridReader = await connection.QueryMultipleAsync(command);

        long totalCount = await gridReader.ReadSingleAsync<long>();
        IEnumerable<DnsResponseEventEntity> items = await gridReader.ReadAsync<DnsResponseEventEntity>();

        _logger.LogInformation(
            "Retrieved page {PageNumber} with {Count} records (Total dataset size: {TotalCount})",
            targetPage,
            items.Count(),
            totalCount);

        return PagedResult<DnsResponseEventEntity>.Create(items, totalCount, targetPage, targetSize);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        const string sql = "DELETE FROM dns_response_events WHERE id = @Id;";

        using IDbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        _logger.LogDebug("Deleting DNS response event record {Id}", id);

        var command = new CommandDefinition(
            sql,
            new { Id = id },
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        int rowsAffected = await connection.ExecuteAsync(command);
        bool deleted = rowsAffected > 0;

        if (deleted)
        {
            _logger.LogInformation("Successfully deleted DNS response event record {Id}", id);
        }
        else
        {
            _logger.LogWarning("Deletion attempt failed. DNS response event record {Id} was not found", id);
        }

        return deleted;
    }
}
