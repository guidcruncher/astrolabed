namespace Astrolabed.Data.Repositories;

using System.Data.Common;

using Astrolabed.Core.Network;
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
    public async Task<IReadOnlyCollection<DnsHourlyEventSummary>> GetHourlyEventSummariesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT (start_time_utc / 3600) % 24 AS EventHour,
                   blocked AS Blocked,
                   COUNT(*) AS TotalEvents
            FROM dns_response_events
            GROUP BY 1, 2
            ORDER BY EventHour;
            """;

        await using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        LogFetchingHourlyEventSummaries(_logger);

        var command = new CommandDefinition(
            sql,
            commandTimeout: _databaseOptions.CommandTimeoutSeconds,
            cancellationToken: cancellationToken);

        IEnumerable<DnsHourlyEventSummaryEntity> entities = await connection.QueryAsync<DnsHourlyEventSummaryEntity>(command);

        var existingData = entities
            .Select(entity => entity.ToDomain())
            .ToDictionary(summary => (summary.EventHour, summary.IsBlocked));

        var completeSummaries = new List<DnsHourlyEventSummary>(48);

        for (int hour = 0; hour < 24; hour++)
        {
            foreach (bool isBlocked in (bool[])[false, true])
            {
                if (existingData.TryGetValue((hour, isBlocked), out var existingSummary))
                {
                    completeSummaries.Add(existingSummary);
                }
                else
                {
                    completeSummaries.Add(new DnsHourlyEventSummary
                    {
                        EventHour = hour,
                        IsBlocked = isBlocked,
                        TotalEvents = 0
                    });
                }
            }
        }

        return completeSummaries.AsReadOnly();
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Fetching hourly DNS event metrics grouped by hour and blocked status")]
    private static partial void LogFetchingHourlyEventSummaries(ILogger logger);
}

