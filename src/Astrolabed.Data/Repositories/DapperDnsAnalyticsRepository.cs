namespace Astrolabed.Data.Repositories;

using System.Data.Common;

using Astrolabed.Data;
using Astrolabed.Data.Options;

using Dapper;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Provides a cross-compatible implementation of <see cref="IDnsAnalyticsRepository"/> using Dapper ADO.NET abstractions.
/// Compatible with both PostgreSQL and SQLite (v3.38+) database backends.
/// </summary>
public sealed class DapperDnsAnalyticsRepository : IDnsAnalyticsRepository
{

    private readonly IDbConnectionFactory _connectionFactory;
    private readonly DatabaseOptions _databaseOptions;
    private readonly ILogger<DapperDnsAnalyticsRepository> _logger;

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
                        (CAST(SUM(blocked) AS DECIMAL) / NULLIF(COUNT(*), 0)) * 100, 2
                    ), 0.0
                )
            FROM dns_response_events
            WHERE start_time_utc >= @StartTimeUtc;
            """;

        _logger.LogDebug("Calculating DNS block rate for queries since epoch {StartTimeUtc}.", startTimeUtc);
        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await connection.ExecuteScalarAsync<double>(new CommandDefinition(sql, new { StartTimeUtc = startTimeUtc }, cancellationToken: cancellationToken));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RetryStormResult>> GetRetryStormsAsync(long startTimeUtc, int limit = 50, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT 
                client_ip AS ClientIp,
                client_name AS ClientName,
                question_name AS QuestionName,
                COUNT(*) AS QueryCount
            FROM dns_response_events
            WHERE blocked = 1
              AND start_time_utc >= @StartTimeUtc
            GROUP BY client_ip, client_name, question_name
            HAVING COUNT(*) > @Limit
            ORDER BY QueryCount DESC;
            """;

        _logger.LogDebug("Querying retry storms since epoch {StartTimeUtc} with limit threshold {Limit}.", startTimeUtc, limit);
        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);

        return await connection.QueryAsync<RetryStormResult>(new CommandDefinition(sql, new { StartTimeUtc = startTimeUtc, Limit = limit }, cancellationToken: cancellationToken));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CnameCloakingResult>> GetCnameCloakingMatchesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT 
                d.question_name AS QuestionName,
                d.answer_data AS AnswerData,
                COUNT(*) AS HitCount
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

        _logger.LogDebug("Executing CNAME cloaking detection query against resolved JSON arrays.");
        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken).ConfigureAwait(false);

        return await connection.QueryAsync<CnameCloakingResult>(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }
}

