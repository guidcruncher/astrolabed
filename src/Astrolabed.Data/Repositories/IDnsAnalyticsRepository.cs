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
/// Represents the aggregation result for a detected CNAME cloaking pattern.
/// </summary>
public sealed record CnameCloakingResult
{
    /// <summary>
    /// Gets or sets the queried first-party domain name.
    /// </summary>
    public string QuestionName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the raw JSON string containing resolved CNAME target strings.
    /// </summary>
    public string AnswerData { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the total number of unblocked hits recorded for this alias target.
    /// </summary>
    public long HitCount { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CnameCloakingResult"/> class.
    /// Required by Dapper for parameterless default object materialization.
    /// </summary>
    public CnameCloakingResult()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CnameCloakingResult"/> class with parameters.
    /// </summary>
    /// <param name="questionName">The queried first-party domain name.</param>
    /// <param name="answerData">The raw JSON string containing resolved CNAME target strings.</param>
    /// <param name="hitCount">The total number of unblocked hits recorded for this alias target.</param>
    public CnameCloakingResult(string questionName, string answerData, long hitCount)
    {
        QuestionName = questionName;
        AnswerData = answerData;
        HitCount = hitCount;
    }
}


/// <summary>
/// Represents the query aggregate result for detected DNS retry storm activity.
/// </summary>
public sealed class RetryStormResult
{
    /// <summary>
    /// Gets or sets the IP address of the client triggering the retry storm.
    /// </summary>
    public string ClientIp { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the hostname or friendly name of the client triggering the retry storm.
    /// </summary>
    public string ClientName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the DNS query target name.
    /// </summary>
    public string QuestionName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the total number of blocked query attempts recorded.
    /// </summary>
    public long QueryCount { get; set; }
    /// <summary>
    /// Initializes a new instance of the <see cref="RetryStormResult"/> class.
    /// Required by Dapper for default parameterless object materialization.
    /// </summary>
    public RetryStormResult()
    {
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="RetryStormResult"/> class with parameters.
    /// </summary>
    /// <param name="clientIp">The IP address of the client.</param>
    /// <param name="clientName">The resolved name of the client.</param>
    /// <param name="questionName">The DNS question domain name.</param>
    /// <param name="queryCount">The aggregated count of queries.</param>
    public RetryStormResult(string clientIp, string clientName, string questionName, long queryCount)
    {
        ClientIp = clientIp;
        ClientName = clientName;
        QuestionName = questionName;
        QueryCount = queryCount;
    }
}
