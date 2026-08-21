namespace Astrolabed.Data.Services;

using System.Data;
using System.Data.Common;
using System.IO;

using Astrolabed.Data.Options;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Npgsql;

/// <summary>
/// Ensures database existence and applies schema scripts for SQLite and PostgreSQL database providers.
/// </summary>
public sealed class DatabaseInitializer : IDatabaseInitializer
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISchemaProvider _schemaProvider;
    private readonly DatabaseOptions _options;
    private readonly ILogger<DatabaseInitializer> _logger;

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

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting database initialization for provider: {Provider}", _options.Provider);

        if (_options.Provider == Provider.PostgreSql)
        {
            await EnsurePostgreSqlDatabaseExistsAsync(cancellationToken);
        }
        else if (_options.Provider == Provider.Sqlite)
        {
            EnsureSqliteDirectoryExists();
        }

        string schemaSql = await _schemaProvider.GetSchemaSqlAsync(cancellationToken);

        _logger.LogDebug("Opening target connection to execute schema deployment...");

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

        _logger.LogInformation("Successfully initialized database schema for provider: {Provider}", _options.Provider);
    }

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
            _logger.LogInformation("Creating missing directory hierarchy for SQLite database at '{DirectoryPath}'", directoryPath);
            Directory.CreateDirectory(directoryPath);
        }
    }

    private async Task EnsurePostgreSqlDatabaseExistsAsync(CancellationToken cancellationToken)
    {
        var builder = new NpgsqlConnectionStringBuilder(_options.ConnectionString);
        string targetDatabaseName = builder.Database;

        if (string.IsNullOrWhiteSpace(targetDatabaseName))
        {
            _logger.LogWarning("PostgreSQL connection string does not specify a target database name.");
            return;
        }

        builder.Database = "postgres";

        _logger.LogDebug("Checking if PostgreSQL database '{DatabaseName}' exists...", targetDatabaseName);

        using var adminConnection = new NpgsqlConnection(builder.ConnectionString);
        await adminConnection.OpenAsync(cancellationToken);

        using (var checkCommand = adminConnection.CreateCommand())
        {
            checkCommand.CommandText = "SELECT 1 FROM pg_database WHERE datname = @dbName;";
            checkCommand.Parameters.AddWithValue("@dbName", targetDatabaseName);

            object? result = await checkCommand.ExecuteScalarAsync(cancellationToken);

            if (result is null || result == DBNull.Value)
            {
                _logger.LogInformation("Database '{DatabaseName}' does not exist. Creating database...", targetDatabaseName);

                using var createCommand = adminConnection.CreateCommand();
                createCommand.CommandText = $"CREATE DATABASE \"{targetDatabaseName.Replace("\"", "\"\"")}\";";
                await createCommand.ExecuteNonQueryAsync(cancellationToken);

                _logger.LogInformation("Database '{DatabaseName}' created successfully.", targetDatabaseName);
            }
            else
            {
                _logger.LogDebug("Database '{DatabaseName}' already exists.", targetDatabaseName);
            }
        }
    }
}

