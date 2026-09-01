namespace Astrolabed.Data.Repositories;

using System.Data.Common;

using Astrolabed.Data.Models;
using Astrolabed.Data.Options;

using Dapper;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// High-performance Dapper implementation for managing <see cref="DnsListEntity"/> persistence
/// across relational database providers.
/// </summary>
/// <remarks>
/// Optimized for .NET 10 asynchronous database I/O, allocation-free parameter passing,
/// and source-generated structured logging.
/// </remarks>
/// <param name="connectionFactory">The database connection factory providing asynchronous database access.</param>
/// <param name="databaseOptions">Database configuration settings, including command execution timeouts.</param>
/// <param name="logger">Structured logging instance for diagnostic and operational logs.</param>
public sealed partial class DapperDnsListRepository(
    IDbConnectionFactory connectionFactory,
    IOptions<DatabaseOptions> databaseOptions,
    ILogger<DapperDnsListRepository> logger) : IDnsListRepository
{
    private readonly IDbConnectionFactory _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    private readonly DatabaseOptions _databaseOptions = databaseOptions?.Value ?? throw new ArgumentNullException(nameof(databaseOptions));
    private readonly ILogger<DapperDnsListRepository> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<DnsListEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id AS Id,
                   name AS Name,
                   path AS Path
            FROM dns_lists
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

        return await connection.QuerySingleOrDefaultAsync<DnsListEntity>(command).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DnsListEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id AS Id,
                   name AS Name,
                   path AS Path
            FROM dns_lists
            ORDER BY id ASC;
            """;

        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);

        LogFetchingAllRecords(_logger);

        var command = new CommandDefinition(
            sql,
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        IEnumerable<DnsListEntity> result = await connection.QueryAsync<DnsListEntity>(command).ConfigureAwait(false);
        List<DnsListEntity> items = result.ToList();

        LogFetchedAllRecordsSuccessfully(_logger, items.Count);

        return items.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task AddAsync(DnsListEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        const string sql = """
            INSERT INTO dns_lists (
                id, name, path
            ) VALUES (
                @Id, @Name, @Path
            );
            """;

        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);

        LogInsertingListRecord(_logger, entity.Id);

        var parameters = new DynamicParameters();
        parameters.Add("Id", entity.Id);
        parameters.Add("Name", entity.Name);
        parameters.Add("Path", entity.Path);

        var command = new CommandDefinition(
            sql,
            parameters,
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command).ConfigureAwait(false);

        LogInsertedListRecordSuccessfully(_logger, entity.Id);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(DnsListEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        const string sql = """
            UPDATE dns_lists
            SET name = @Name,
                path = @Path
            WHERE id = @Id;
            """;

        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);

        LogUpdatingListRecord(_logger, entity.Id);

        var parameters = new DynamicParameters();
        parameters.Add("Id", entity.Id);
        parameters.Add("Name", entity.Name);
        parameters.Add("Path", entity.Path);

        var command = new CommandDefinition(
            sql,
            parameters,
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        int rowsAffected = await connection.ExecuteAsync(command).ConfigureAwait(false);
        bool updated = rowsAffected > 0;

        if (updated)
        {
            LogUpdatedListRecordSuccessfully(_logger, entity.Id);
        }
        else
        {
            LogUpdateFailedNotFound(_logger, entity.Id);
        }

        return updated;
    }

    /// <inheritdoc />
    public async Task UpsertAsync(DnsListEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        const string sql = """
            INSERT INTO dns_lists (
                id, name, path
            ) VALUES (
                @Id, @Name, @Path
            )
            ON CONFLICT(id) DO UPDATE SET
                name = EXCLUDED.name,
                path = EXCLUDED.path;
            """;

        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);

        LogUpsertingListRecord(_logger, entity.Id);

        var parameters = new DynamicParameters();
        parameters.Add("Id", entity.Id);
        parameters.Add("Name", entity.Name);
        parameters.Add("Path", entity.Path);

        var command = new CommandDefinition(
            sql,
            parameters,
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command).ConfigureAwait(false);

        LogUpsertedListRecordSuccessfully(_logger, entity.Id);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM dns_lists WHERE id = @Id;";

        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);

        LogDeletingListRecord(_logger, id);

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
            LogDeletedListRecordSuccessfully(_logger, id);
        }
        else
        {
            LogDeleteFailedNotFound(_logger, id);
        }

        return deleted;
    }

    [LoggerMessage(EventId = 301, Level = LogLevel.Debug, Message = "Fetching DNS list record by ID {Id}")]
    private static partial void LogFetchingById(ILogger logger, int id);

    [LoggerMessage(EventId = 302, Level = LogLevel.Debug, Message = "Fetching all DNS list records")]
    private static partial void LogFetchingAllRecords(ILogger logger);

    [LoggerMessage(EventId = 303, Level = LogLevel.Information, Message = "Successfully retrieved {Count} DNS list records")]
    private static partial void LogFetchedAllRecordsSuccessfully(ILogger logger, int count);

    [LoggerMessage(EventId = 304, Level = LogLevel.Debug, Message = "Inserting DNS list record {Id}")]
    private static partial void LogInsertingListRecord(ILogger logger, int id);

    [LoggerMessage(EventId = 305, Level = LogLevel.Information, Message = "Successfully inserted DNS list record {Id}")]
    private static partial void LogInsertedListRecordSuccessfully(ILogger logger, int id);

    [LoggerMessage(EventId = 306, Level = LogLevel.Debug, Message = "Updating DNS list record {Id}")]
    private static partial void LogUpdatingListRecord(ILogger logger, int id);

    [LoggerMessage(EventId = 307, Level = LogLevel.Information, Message = "Successfully updated DNS list record {Id}")]
    private static partial void LogUpdatedListRecordSuccessfully(ILogger logger, int id);

    [LoggerMessage(EventId = 308, Level = LogLevel.Warning, Message = "Update attempt failed. DNS list record {Id} was not found")]
    private static partial void LogUpdateFailedNotFound(ILogger logger, int id);

    [LoggerMessage(EventId = 309, Level = LogLevel.Debug, Message = "Deleting DNS list record {Id}")]
    private static partial void LogDeletingListRecord(ILogger logger, int id);

    [LoggerMessage(EventId = 310, Level = LogLevel.Information, Message = "Successfully deleted DNS list record {Id}")]
    private static partial void LogDeletedListRecordSuccessfully(ILogger logger, int id);

    [LoggerMessage(EventId = 311, Level = LogLevel.Warning, Message = "Deletion attempt failed. DNS list record {Id} was not found")]
    private static partial void LogDeleteFailedNotFound(ILogger logger, int id);

    [LoggerMessage(EventId = 312, Level = LogLevel.Debug, Message = "Upserting DNS list record {Id}")]
    private static partial void LogUpsertingListRecord(ILogger logger, int id);

    [LoggerMessage(EventId = 313, Level = LogLevel.Information, Message = "Successfully upserted DNS list record {Id}")]
    private static partial void LogUpsertedListRecordSuccessfully(ILogger logger, int id);
}
