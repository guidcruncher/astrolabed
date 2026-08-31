namespace Astrolabed.Data.Repositories;

/// <summary>
/// Defines the data access contract for storing and querying DNS response log events 
/// and executing ad-blocking effectiveness analytics.
/// </summary>
public interface IDnsAnalyticsRepository
{
    /// <summary>
    /// Calculates the overall network block rate percentage for queries recorded on or after the specified timestamp.
    /// </summary>
    /// <param name="startTimeUtc">The starting Unix epoch timestamp in milliseconds.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>The block rate as a percentage value between 0.00 and 100.00.</returns>
    Task<double> GetBlockRateAsync(long startTimeUtc, CancellationToken cancellationToken = default);

    /// <summary>
    /// Identifies client endpoints exhibiting excessive blocked query volumes within a given timeframe, signaling potential retry storms.
    /// </summary>
    /// <param name="startTimeUtc">The starting Unix epoch timestamp in milliseconds.</param>
    /// <param name="limit">The minimum query count threshold required to classify a sequence as a retry storm. Defaults to 50.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A collection of <see cref="RetryStormResult"/> instances ordered by highest query count.</returns>
    Task<IEnumerable<RetryStormResult>> GetRetryStormsAsync(long startTimeUtc, int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects potential CNAME cloaking patterns where unblocked domain requests resolve to known third-party tracking target aliases.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A collection of <see cref="CnameCloakingResult"/> instances representing detected cloaked aliases.</returns>
    Task<IEnumerable<CnameCloakingResult>> GetCnameCloakingMatchesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the aggregation result for a detected client retry storm event.
/// </summary>
/// <param name="ClientIp">The IP address of the client triggering the retry burst.</param>
/// <param name="ClientName">The resolve hostname or friendly display name of the client, if available.</param>
/// <param name="QuestionName">The DNS domain name repeatedly requested by the client.</param>
/// <param name="QueryCount">The total number of blocked requests recorded during the period.</param>
public record RetryStormResult(string ClientIp, string? ClientName, string QuestionName, long QueryCount);

/// <summary>
/// Represents the aggregation result for a detected CNAME cloaking pattern.
/// </summary>
/// <param name="QuestionName">The queried first-party domain name.</param>
/// <param name="AnswerData">The raw JSON string containing resolved CNAME target strings.</param>
/// <param name="HitCount">The total number of unblocked hits recorded for this alias target.</param>
public record CnameCloakingResult(string QuestionName, string AnswerData, long HitCount);
