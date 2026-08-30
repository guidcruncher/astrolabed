namespace Astrolabed.Data.Repositories;

using Astrolabed.Data.Models;

/// <summary>
/// Service contract for querying analytical metrics and event statistics.
/// </summary>
public interface IStatsRepository
{
    /// <summary>
    /// Gets aggregated DNS event metrics grouped into 24 hourly records (0–23 UTC),
    /// detailing blocked and allowed counts per hour.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A collection of exactly 24 hourly event metrics.</returns>
    Task<IReadOnlyCollection<DnsHourlyEventSummary>> GetHourlyEventSummariesAsync(CancellationToken cancellationToken = default);
}
