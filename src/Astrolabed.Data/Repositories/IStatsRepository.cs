namespace Astrolabed.Data.Repositories;

using Astrolabed.Data.Models;

/// <summary>
/// Service contract for querying analytical metrics and event statistics.
/// </summary>
public interface IStatsRepository
{
    /// <summary>
    /// Gets aggregated event counts grouped by hour of the day and blocked status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of hourly event metrics.</returns>
    Task<IReadOnlyCollection<DnsHourlyEventSummary>> GetHourlyEventSummariesAsync(CancellationToken cancellationToken = default);
}

