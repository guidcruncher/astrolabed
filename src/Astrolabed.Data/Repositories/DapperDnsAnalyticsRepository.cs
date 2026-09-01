// File: src/Astrolabed.Data/Repositories/DapperDnsAnalyticsRepository.cs
using System.Data.Common;

using Astrolabed.Data.Options;

using Dapper;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Data.Repositories;

/// <summary>
/// Provides a cross-compatible implementation of <see cref="IDnsAnalyticsRepository"/> using Dapper ADO.NET abstractions.
/// Compatible with both PostgreSQL and SQLite (v3.38+) database backends.
/// </summary>
public sealed partial class DapperDnsAnalyticsRepository : IDnsAnalyticsRepository
{
    /// <summary>
    /// The database connection factory used to acquire asynchronous connections.
    /// </summary>
    private readonly IDbConnectionFactory _connectionFactory;

    /// <summary>
    /// The database configuration options, including timeout settings.
    /// </summary>
    private readonly DatabaseOptions _databaseOptions;

    /// <summary>
    /// The structured logging instance for repository diagnostics.
    /// </summary>
    private readonly ILogger<DapperDnsAnalyticsRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DapperDnsAnalyticsRepository"/> class.
    /// </summary>
    /// <param name="connectionFactory">The factory used to establish database connections.</param>
    /// <param name="databaseOptions">The options containing database configuration settings.</param>
    /// <param name="logger">The logger for diagnostic messages.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionFactory"/>, <paramref name="databaseOptions"/>, or <paramref name="logger"/> is <see langword="null"/>.</exception>
    public DapperDnsAnalyticsRepository(
        IDbConnectionFactory connectionFactory,
        IOptions<DatabaseOptions> databaseOptions,
        ILogger<DapperDnsAnalyticsRepository> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _databaseOptions = databaseOptions?.Value ?? throw new ArgumentNullException(nameof(databaseOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<double> GetBlockRateAsync(long startTimeUtc, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT 
                COALESCE(
                    ROUND(
                        (CAST((SUM(blocked) * 1.0) AS DECIMAL) / NULLIF(COUNT(*), 0)) * 100, 2
                    ), 0.0
                )
            FROM dns_response_events
            WHERE start_time_utc >= @StartTimeUtc;
            """;

        LogCalculatingBlockRate(_logger, startTimeUtc);
        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);

        var command = new CommandDefinition(sql, new { StartTimeUtc = startTimeUtc }, commandTimeout: _databaseOptions.CommandTimeoutSeconds, cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<double>(command).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RetryStormResult>> GetRetryStormsAsync(long startTimeUtc, int limit = 50, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT 
                client_ip AS ClientIp,
                client_name AS ClientName,
                question_name AS QuestionName,
                CAST(COUNT(*) AS BIGINT) AS QueryCount
            FROM dns_response_events
            WHERE blocked = 1
              AND start_time_utc >= @StartTimeUtc
            GROUP BY client_ip, client_name, question_name
            HAVING COUNT(*) >= @Limit
            ORDER BY QueryCount DESC;
            """;

        LogQueryingRetryStorms(_logger, startTimeUtc, limit);
        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);

        var command = new CommandDefinition(sql, new { StartTimeUtc = startTimeUtc, Limit = limit }, commandTimeout: _databaseOptions.CommandTimeoutSeconds, cancellationToken: cancellationToken);
        return await connection.QueryAsync<RetryStormResult>(command).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CnameCloakingResult>> GetCnameCloakingMatchesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT 
                d.question_name AS QuestionName,
                d.answer_data AS AnswerData,
                CAST(COUNT(*) AS BIGINT) AS HitCount
            FROM dns_response_events d
            JOIN json_each(d.answer_data) j ON 1=1
            WHERE d.blocked = 0
              AND d.answer_data IS NOT NULL
              AND (
                  j.value LIKE '%.omtrdc.net' 
               OR j.value LIKE '%.adtech.com'
               OR j.value LIKE '%.criteo.com'
              )
            GROUP BY d.question_name, d.answer_data
            ORDER BY HitCount DESC;
            """;

        LogExecutingCnameCloakingQuery(_logger);
        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);

        var command = new CommandDefinition(sql, commandTimeout: _databaseOptions.CommandTimeoutSeconds, cancellationToken: cancellationToken);
        return await connection.QueryAsync<CnameCloakingResult>(command).ConfigureAwait(false);
    }

    [LoggerMessage(
        EventId = 201,
        Level = LogLevel.Debug,
        Message = "Calculating DNS block rate for queries since epoch {StartTimeUtc}.")]
    private static partial void LogCalculatingBlockRate(ILogger logger, long startTimeUtc);

    [LoggerMessage(
        EventId = 202,
        Level = LogLevel.Debug,
        Message = "Querying retry storms since epoch {StartTimeUtc} with limit threshold {Limit}.")]
    private static partial void LogQueryingRetryStorms(ILogger logger, long startTimeUtc, int limit);

    [LoggerMessage(
        EventId = 203,
        Level = LogLevel.Debug,
        Message = "Executing CNAME cloaking detection query against resolved JSON arrays.")]
    private static partial void LogExecutingCnameCloakingQuery(ILogger logger);
}
