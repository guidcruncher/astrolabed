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
    /// <param name="startEpoch">Start Epoch to filter by</param>
    /// <param name="endEpoch">End Epoch to filter by</param>
    /// <returns>A collection of exactly 24 hourly event metrics.</returns>
    Task<IReadOnlyCollection<DnsHourlyEventSummary>> GetHourlyEventSummariesAsync(long startEpoch, long endEpoch, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a breakdown of DNS queries grouped by their question type.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <param name="startEpoch">Start Epoch to filter by</param>
    /// <param name="endEpoch">End Epoch to filter by</param>
    /// <returns>A collection of DNS question type summaries detailing counts per record type.</returns>
    Task<IEnumerable<DnsQuestionTypeSummary>> GetQuestionTypeSummary(long startEpoch, long endEpoch, CancellationToken cancellationToken = default);
}
