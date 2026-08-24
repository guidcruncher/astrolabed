// File: src/Astrolabed.Data/Services/DatabaseInitializer.cs
using System.Data;
using System.Data.Common;

using Astrolabed.Data.Options;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Npgsql;

namespace Astrolabed.Data.Services;

/// <summary>
/// Ensures database existence and applies schema scripts for SQLite and PostgreSQL database providers.
/// </summary>
public sealed partial class DatabaseInitializer : IDatabaseInitializer
{
    /// <summary>
    /// Connection factory used to establish database connections.
    /// </summary>
    private readonly IDbConnectionFactory _connectionFactory;

    /// <summary>
    /// Schema provider used to load SQL schema deployment scripts.
    /// </summary>
    private readonly ISchemaProvider _schemaProvider;

    /// <summary>
    /// Database configuration options instance.
    /// </summary>
    private readonly DatabaseOptions _options;

    /// <summary>
    /// Structured logger instance for diagnostics and execution logging.
    /// </summary>
    private readonly ILogger<DatabaseInitializer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseInitializer"/> class.
    /// </summary>
    /// <param name="connectionFactory">Factory used to create database connections.</param>
    /// <param name="schemaProvider">Provider for fetching target database creation SQL scripts.</param>
    /// <param name="options">Monitored or fixed database configuration options.</param>
    /// <param name="logger">Structured logging instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is <c>null</c>.</exception>
    public DatabaseInitializer(
        IDbConnectionFactory connectionFactory,
        ISchemaProvider schemaProvider,
        IOptions<DatabaseOptions> options,
        ILogger<DatabaseInitializer> logger)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        ArgumentNullException.ThrowIfNull(schemaProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _connectionFactory = connectionFactory;
        _schemaProvider = schemaProvider;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Executes database setup asynchronously, ensuring physical target store initialization and schema migration deployment.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to signal operation cancellation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous initialization operation.</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        LogStartingDatabaseInitialization(_logger, _options.Provider);

        if (_options.Provider == Provider.PostgreSql)
        {
            await EnsurePostgreSqlDatabaseExistsAsync(cancellationToken);
        }
        else if (_options.Provider == Provider.Sqlite)
        {
            EnsureSqliteDirectoryExists();
        }

        string schemaSql = await _schemaProvider.GetSchemaSqlAsync(cancellationToken);

        LogOpeningConnectionToExecuteSchema(_logger);

        using IDbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        using IDbCommand command = connection.CreateCommand();

        command.CommandText = schemaSql;
        command.CommandTimeout = _options.CommandTimeoutSeconds;

        if (command is DbCommand dbCommand)
        {
            await dbCommand.ExecuteNonQueryAsync(cancellationToken);
        }
        else
        {
            command.ExecuteNonQuery();
        }

        LogSchemaInitializationSuccess(_logger, _options.Provider);
    }

    /// <summary>
    /// Verifies the presence of directory structures required for SQLite local file database persistence and creates missing directories.
    /// </summary>
    private void EnsureSqliteDirectoryExists()
    {
        var builder = new SqliteConnectionStringBuilder(_options.ConnectionString);
        string dataSource = builder.DataSource;

        if (string.IsNullOrWhiteSpace(dataSource) || dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string? directoryPath = Path.GetDirectoryName(dataSource);

        if (!string.IsNullOrWhiteSpace(directoryPath) && !Directory.Exists(directoryPath))
        {
            LogCreatingSqliteDirectory(_logger, directoryPath);
            Directory.CreateDirectory(directoryPath);
        }
    }

    /// <summary>
    /// Connects to PostgreSQL server administrative database ('postgres') and verifies target database existence, creating it if missing.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to signal operation cancellation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    private async Task EnsurePostgreSqlDatabaseExistsAsync(CancellationToken cancellationToken)
    {
        var builder = new NpgsqlConnectionStringBuilder(_options.ConnectionString);
        string? targetDatabaseName = builder.Database;

        if (string.IsNullOrWhiteSpace(targetDatabaseName))
        {
            LogPostgreSqlDatabaseNameMissing(_logger);
            return;
        }

        builder.Database = "postgres";

        LogCheckingPostgreSqlDatabaseExists(_logger, targetDatabaseName);

        using var adminConnection = new NpgsqlConnection(builder.ConnectionString);
        await adminConnection.OpenAsync(cancellationToken);

        using var checkCommand = adminConnection.CreateCommand();
        checkCommand.CommandText = "SELECT 1 FROM pg_database WHERE datname = @dbName;";
        checkCommand.Parameters.AddWithValue("@dbName", targetDatabaseName);

        object? result = await checkCommand.ExecuteScalarAsync(cancellationToken);

        if (result is null || result == DBNull.Value)
        {
            LogPostgreSqlDatabaseNotFound(_logger, targetDatabaseName);

            using var createCommand = adminConnection.CreateCommand();
            createCommand.CommandText = $"CREATE DATABASE \"{targetDatabaseName.Replace("\"", "\"\"")}\";";
            await createCommand.ExecuteNonQueryAsync(cancellationToken);

            LogPostgreSqlDatabaseCreatedSuccessfully(_logger, targetDatabaseName);
        }
        else
        {
            LogPostgreSqlDatabaseAlreadyExists(_logger, targetDatabaseName);
        }
    }

    [LoggerMessage(
        EventId = 801,
        Level = LogLevel.Information,
        Message = "Starting database initialization for provider: {Provider}")]
    private static partial void LogStartingDatabaseInitialization(ILogger logger, Provider provider);

    [LoggerMessage(
        EventId = 802,
        Level = LogLevel.Debug,
        Message = "Opening target connection to execute schema deployment...")]
    private static partial void LogOpeningConnectionToExecuteSchema(ILogger logger);

    [LoggerMessage(
        EventId = 803,
        Level = LogLevel.Information,
        Message = "Successfully initialized database schema for provider: {Provider}")]
    private static partial void LogSchemaInitializationSuccess(ILogger logger, Provider provider);

    [LoggerMessage(
        EventId = 804,
        Level = LogLevel.Information,
        Message = "Creating missing directory hierarchy for SQLite database at '{DirectoryPath}'")]
    private static partial void LogCreatingSqliteDirectory(ILogger logger, string directoryPath);

    [LoggerMessage(
        EventId = 805,
        Level = LogLevel.Warning,
        Message = "PostgreSQL connection string does not specify a target database name.")]
    private static partial void LogPostgreSqlDatabaseNameMissing(ILogger logger);

    [LoggerMessage(
        EventId = 806,
        Level = LogLevel.Debug,
        Message = "Checking if PostgreSQL database '{DatabaseName}' exists...")]
    private static partial void LogCheckingPostgreSqlDatabaseExists(ILogger logger, string databaseName);

    [LoggerMessage(
        EventId = 807,
        Level = LogLevel.Information,
        Message = "Database '{DatabaseName}' does not exist. Creating database...")]
    private static partial void LogPostgreSqlDatabaseNotFound(ILogger logger, string databaseName);

    [LoggerMessage(
        EventId = 808,
        Level = LogLevel.Information,
        Message = "Database '{DatabaseName}' created successfully.")]
    private static partial void LogPostgreSqlDatabaseCreatedSuccessfully(ILogger logger, string databaseName);

    [LoggerMessage(
        EventId = 809,
        Level = LogLevel.Debug,
        Message = "Database '{DatabaseName}' already exists.")]
    private static partial void LogPostgreSqlDatabaseAlreadyExists(ILogger logger, string databaseName);
}
