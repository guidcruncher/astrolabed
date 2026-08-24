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

/// <remarks>
/// Instantiates database connections asynchronously and exposes <see cref="DbConnection"/> to support
/// non-blocking <see cref="IAsyncDisposable"/> cleanup in consuming repositories.
/// </remarks>
/// <param name="options">Database options containing connection string and provider selection.</param>
/// <param name="logger">Structured logger instance for factory diagnostic logging.</param>
public sealed partial class DbConnectionFactory(
    IOptions<DatabaseOptions> options,
    ILogger<DbConnectionFactory> logger) : IDbConnectionFactory
{
    private readonly DatabaseOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger<DbConnectionFactory> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<DbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        LogCreatingConnection(_logger, _options.Provider);

        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new InvalidOperationException("Database connection string has not been configured.");
        }

        DbConnection connection = _options.Provider switch
        {
            Provider.Sqlite => new SqliteConnection(_options.ConnectionString),
            Provider.PostgreSql => new NpgsqlConnection(_options.ConnectionString),
            _ => throw new InvalidOperationException($"Unsupported database provider specified: {_options.Provider}")
        };

        try
        {
            await connection.OpenAsync(cancellationToken);
            LogConnectionOpenedSuccessfully(_logger, _options.Provider);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    [LoggerMessage(EventId = 101, Level = LogLevel.Debug, Message = "Creating database connection using configured provider: {Provider}")]
    private static partial void LogCreatingConnection(ILogger logger, Provider provider);

    [LoggerMessage(EventId = 102, Level = LogLevel.Trace, Message = "Database connection successfully opened for provider: {Provider}")]
    private static partial void LogConnectionOpenedSuccessfully(ILogger logger, Provider provider);
}
