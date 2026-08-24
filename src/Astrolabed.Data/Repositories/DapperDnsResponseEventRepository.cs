// File: src/Astrolabed.Data/Repositories/DapperDnsResponseEventRepository.cs
using System.Data.Common;

using Astrolabed.Data.Models;
using Astrolabed.Data.Options;
using Astrolabed.Data.Pagination;

using Dapper;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Data.Repositories;

/// <summary>
/// High-performance Dapper implementation for managing <see cref="DnsResponseEventEntity"/> persistence
/// across relational database providers.
/// </summary>
/// <remarks>
/// Optimized for .NET 10 asynchronous database I/O, allocation-free parameter passing,
/// and source-generated structured logging.
/// </remarks>
/// <param name="connectionFactory">The database connection factory providing asynchronous database access.</param>
/// <param name="databaseOptions">Database configuration settings, including command execution timeouts.</param>
/// <param name="logger">Structured logging instance for diagnostic and operational logs.</param>
public sealed partial class DapperDnsResponseEventRepository(
    IDbConnectionFactory connectionFactory,
    IOptions<DatabaseOptions> databaseOptions,
    ILogger<DapperDnsResponseEventRepository> logger) : IDnsResponseEventRepository
{
    private readonly IDbConnectionFactory _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    private readonly DatabaseOptions _databaseOptions = databaseOptions?.Value ?? throw new ArgumentNullException(nameof(databaseOptions));
    private readonly ILogger<DapperDnsResponseEventRepository> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task AddAsync(DnsResponseEventEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        const string sql = """
            INSERT INTO dns_response_events (
                id, start_time_utc, context_id, question_name, question_type,
                client_endpoint, client_name, resolution_source, duration_ms, blocked
            ) VALUES (
                @Id, @StartTimeUtc, @ContextId, @QuestionName, @QuestionType,
                @ClientEndpoint, @ClientName, @ResolutionSource, @DurationMs, @Blocked
            );
            """;

        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);

        LogInsertingEventRecord(_logger, entity.Id);

        var parameters = new DynamicParameters();
        parameters.Add("Id", entity.Id);
        parameters.Add("StartTimeUtc", entity.StartTimeUtc);
        parameters.Add("ContextId", entity.ContextId);
        parameters.Add("QuestionName", entity.QuestionName);
        parameters.Add("QuestionType", entity.QuestionType);
        parameters.Add("ClientEndpoint", entity.ClientEndpoint);
        parameters.Add("ClientName", entity.ClientName);
        parameters.Add("ResolutionSource", entity.ResolutionSource);
        parameters.Add("DurationMs", entity.DurationMs);
        parameters.Add("Blocked", entity.Blocked);

        var command = new CommandDefinition(
            sql,
            parameters,
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command).ConfigureAwait(false);

        LogInsertedEventRecordSuccessfully(_logger, entity.Id);
    }

    /// <inheritdoc />
    public async Task<DnsResponseEventEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        const string sql = """
            SELECT id, start_time_utc, context_id, question_name, question_type,
                   client_endpoint, client_name, resolution_source, duration_ms, blocked
            FROM dns_response_events
            WHERE id = @Id;
            """;

        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);

        LogFetchingById(_logger, id);

        var parameters = new DynamicParameters();
        parameters.Add("Id", id);

        var command = new CommandDefinition(
            sql,
            parameters,
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<DnsResponseEventEntity>(command).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PagedResult<DnsResponseEventEntity>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        int targetPage = pageNumber < 1 ? 1 : pageNumber;
        int targetSize = pageSize < 1 ? 10 : Math.Min(pageSize, 100);
        int offset = (targetPage - 1) * targetSize;

        const string sql = """
            SELECT COUNT(1) FROM dns_response_events;

            SELECT id, start_time_utc, context_id, question_name, question_type,
                   client_endpoint, client_name, resolution_source, duration_ms, blocked
            FROM dns_response_events
            ORDER BY start_time_utc DESC
            LIMIT @PageSize OFFSET @Offset;
            """;

        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);

        LogExecutingPagedQuery(_logger, targetPage, targetSize);

        var parameters = new DynamicParameters();
        parameters.Add("PageSize", targetSize);
        parameters.Add("Offset", offset);

        var command = new CommandDefinition(
            sql,
            parameters,
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        await using SqlMapper.GridReader gridReader = await connection.QueryMultipleAsync(command).ConfigureAwait(false);

        long totalCount = await gridReader.ReadSingleAsync<long>().ConfigureAwait(false);
        IEnumerable<DnsResponseEventEntity> readItems = await gridReader.ReadAsync<DnsResponseEventEntity>().ConfigureAwait(false);
        List<DnsResponseEventEntity> items = readItems.ToList();

        LogRetrievedPagedResults(_logger, targetPage, items.Count, totalCount);

        return PagedResult<DnsResponseEventEntity>.Create(items, totalCount, targetPage, targetSize);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        const string sql = "DELETE FROM dns_response_events WHERE id = @Id;";

        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);

        LogDeletingRecord(_logger, id);

        var parameters = new DynamicParameters();
        parameters.Add("Id", id);

        var command = new CommandDefinition(
            sql,
            parameters,
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        int rowsAffected = await connection.ExecuteAsync(command).ConfigureAwait(false);
        bool deleted = rowsAffected > 0;

        if (deleted)
        {
            LogDeletedRecordSuccessfully(_logger, id);
        }
        else
        {
            LogDeleteFailedNotFound(_logger, id);
        }

        return deleted;
    }

    /// <inheritdoc />
    public async Task CleanOldDataAsync(CancellationToken cancellationToken = default)
    {
        long cutoff = DateTimeOffset.UtcNow.AddDays(-7).ToUnixTimeSeconds();
        const string sql = "DELETE FROM dns_response_events WHERE start_time_utc < @Cutoff;";

        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);

        LogCleaningOldRecords(_logger, cutoff);

        var parameters = new DynamicParameters();
        parameters.Add("Cutoff", cutoff);

        var command = new CommandDefinition(
            sql,
            parameters,
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        int rowsAffected = await connection.ExecuteAsync(command).ConfigureAwait(false);

        if (rowsAffected > 0)
        {
            LogCleanedOldRecordsSuccessfully(_logger, rowsAffected);
        }
        else
        {
            LogNoOldRecordsFoundToClean(_logger, cutoff);
        }
    }

    [LoggerMessage(EventId = 201, Level = LogLevel.Debug, Message = "Inserting DNS response event record {Id}")]
    private static partial void LogInsertingEventRecord(ILogger logger, string id);

    [LoggerMessage(EventId = 202, Level = LogLevel.Information, Message = "Successfully inserted DNS response event record {Id}")]
    private static partial void LogInsertedEventRecordSuccessfully(ILogger logger, string id);

    [LoggerMessage(EventId = 203, Level = LogLevel.Debug, Message = "Fetching DNS response event record by ID {Id}")]
    private static partial void LogFetchingById(ILogger logger, string id);

    [LoggerMessage(EventId = 204, Level = LogLevel.Debug, Message = "Executing paged SELECT query. PageNumber: {PageNumber}, PageSize: {PageSize}")]
    private static partial void LogExecutingPagedQuery(ILogger logger, int pageNumber, int pageSize);

    [LoggerMessage(EventId = 205, Level = LogLevel.Information, Message = "Retrieved page {PageNumber} with {Count} records (Total dataset size: {TotalCount})")]
    private static partial void LogRetrievedPagedResults(ILogger logger, int pageNumber, int count, long totalCount);

    [LoggerMessage(EventId = 206, Level = LogLevel.Debug, Message = "Deleting DNS response event record {Id}")]
    private static partial void LogDeletingRecord(ILogger logger, string id);

    [LoggerMessage(EventId = 207, Level = LogLevel.Information, Message = "Successfully deleted DNS response event record {Id}")]
    private static partial void LogDeletedRecordSuccessfully(ILogger logger, string id);

    [LoggerMessage(EventId = 208, Level = LogLevel.Warning, Message = "Deletion attempt failed. DNS response event record {Id} was not found")]
    private static partial void LogDeleteFailedNotFound(ILogger logger, string id);

    [LoggerMessage(EventId = 209, Level = LogLevel.Debug, Message = "Deleting old DNS Response records before {Cutoff}")]
    private static partial void LogCleaningOldRecords(ILogger logger, long cutoff);

    [LoggerMessage(EventId = 210, Level = LogLevel.Information, Message = "Successfully deleted {RowsAffected} DNS response event records")]
    private static partial void LogCleanedOldRecordsSuccessfully(ILogger logger, int rowsAffected);

    [LoggerMessage(EventId = 211, Level = LogLevel.Warning, Message = "Could not find any old DNS response event records before {Cutoff}")]
    private static partial void LogNoOldRecordsFoundToClean(ILogger logger, long cutoff);
}
