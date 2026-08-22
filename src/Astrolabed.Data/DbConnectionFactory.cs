using System.Data;
using System.Data.Common;

using Astrolabed.Data.Options;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Npgsql;

namespace Astrolabed.Data;

/// <summary>
/// Implements connection instantiation for PostgreSQL and SQLite based on explicit provider configuration.
/// </summary>
public sealed class DbConnectionFactory : IDbConnectionFactory
{
    private readonly DatabaseOptions _options;
    private readonly ILogger<DbConnectionFactory> _logger;

    public DbConnectionFactory(
        IOptions<DatabaseOptions> options,
        ILogger<DbConnectionFactory> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new InvalidOperationException("Database connection string has not been configured.");
        }
    }

    public async Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating database connection using configured provider: {Provider}", _options.Provider);

        DbConnection connection = _options.Provider switch
        {
            Provider.Sqlite => new SqliteConnection(_options.ConnectionString),
            Provider.PostgreSql => new NpgsqlConnection(_options.ConnectionString),
            _ => throw new InvalidOperationException($"Unsupported database provider specified: {_options.Provider}")
        };

        await connection.OpenAsync(cancellationToken);

        _logger.LogTrace("Database connection successfully opened for provider: {Provider}", _options.Provider);

        return connection;
    }
}
