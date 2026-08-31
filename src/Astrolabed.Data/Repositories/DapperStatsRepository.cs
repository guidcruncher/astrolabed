namespace Astrolabed.Data.Repositories;

using System.Data.Common;

using Astrolabed.Data.Models;
using Astrolabed.Data.Options;

using Dapper;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


/// <summary>
/// High-performance Dapper implementation for querying analytical statistics and DNS metrics.
/// </summary>
/// <remarks>
/// Targets .NET 10 features including primary constructors, asynchronous disposable database contexts,
/// structural parameter bindings to prevent GC allocations, and compile-time logger source generators.
/// </remarks>
/// <param name="connectionFactory">The asynchronous database connection factory.</param>
/// <param name="databaseOptions">Configuration options containing database operational settings.</param>
/// <param name="logger">Structured logger instance for diagnostic output.</param>
public sealed partial class DapperStatsRepository(
    IDbConnectionFactory connectionFactory,
    IOptions<DatabaseOptions> databaseOptions,
    ILogger<DapperStatsRepository> logger) : IStatsRepository
{
    private readonly IDbConnectionFactory _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    private readonly DatabaseOptions _databaseOptions = databaseOptions?.Value ?? throw new ArgumentNullException(nameof(databaseOptions));
    private readonly ILogger<DapperStatsRepository> _logger = logger ?? throw new ArgumentNullException(nameof(logger));


    /// <inheritdoc />
    public async Task<IEnumerable<DnsQuestionTypeSummary>> GetQuestionTypeSummary(long startEpoch, long endEpoch, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT COUNT(*) AS Total, question_type AS QuestionType
            FROM dns_response_events
             WHERE start_time_utc >= @StartEpoch AND start_time_utc <= @EndEpoch
            GROUP BY question_type
            ORDER BY question_type;
            """;

        var parameters = new DynamicParameters();
        parameters.Add("StartEpoch", startEpoch);
        parameters.Add("EndEpoch", endEpoch);

        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var command = new CommandDefinition(
            sql,
        parameters,
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        IEnumerable<DnsQuestionTypeSummary> entities = await connection.QueryAsync<DnsQuestionTypeSummary>(command);
        return entities;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<DnsHourlyEventSummary>> GetHourlyEventSummariesAsync(long startEpoch, long endEpoch, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT (start_time_utc / 3600) % 24 AS EventHour,
                   SUM(CASE WHEN blocked <> 0 THEN 1 ELSE 0 END) AS Blocked,
                   SUM(CASE WHEN blocked = 0 THEN 1 ELSE 0 END) AS Allowed
            WHERE start_time_utc >= @StartEpoch AND start_time_utc <= @EndEpoch 
            FROM dns_response_events
            GROUP BY 1
            ORDER BY EventHour;
            """;

        var parameters = new DynamicParameters();
        parameters.Add("StartEpoch", startEpoch);
        parameters.Add("EndEpoch", endEpoch);

        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        LogFetchingHourlyEventSummaries(_logger);

        var command = new CommandDefinition(
            sql,
        parameters,
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        IEnumerable<DnsHourlyEventSummaryEntity> entities = await connection.QueryAsync<DnsHourlyEventSummaryEntity>(command);

        var existingData = entities
            .Select(entity => entity.ToDomain())
            .ToDictionary(summary => summary.EventHour);

        var completeSummaries = new List<DnsHourlyEventSummary>(24);

        for (int hour = 0; hour < 24; hour++)
        {
            if (existingData.TryGetValue(hour, out var existingSummary))
            {
                completeSummaries.Add(existingSummary);
            }
            else
            {
                completeSummaries.Add(new DnsHourlyEventSummary
                {
                    EventHour = hour,
                    Blocked = 0,
                    Allowed = 0
                });
            }
        }

        return completeSummaries.AsReadOnly();
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Fetching hourly DNS event metrics grouped into one record per hour.")]
    private static partial void LogFetchingHourlyEventSummaries(ILogger logger);
}
