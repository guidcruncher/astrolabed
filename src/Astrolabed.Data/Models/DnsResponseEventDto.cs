namespace Astrolabed.Data.Models;

/// <summary>
/// Data Transfer Object representing a persisted DNS response event.
/// </summary>
/// <param name="Id">The unique primary key identifier string of the log record.</param>
/// <param name="StartTimeUtc">The query execution start time represented as a UTC <see cref="DateTimeOffset"/>.</param>
/// <param name="ContextId">The unique context tracking identifier string associated with the query request.</param>
/// <param name="QuestionName">The queried domain name string (e.g., "example.com").</param>
/// <param name="QuestionType">The queried DNS record type string (e.g., "A", "AAAA", "HTTPS").</param>
/// <param name="ClientAddress">The client Address IP and port string representation (e.g., "192.168.1.100:54321").</param>
/// <param name="ClientName">The resolved name or friendly display name of the client making the DNS request, if available.</param>
/// <param name="ResolutionSource">The resolution outcome source description tag (e.g., "CACHE", "UPSTREAM", "BLOCKLIST").</param>
/// <param name="Rcode">The standard DNS response code (e.g., "NOERROR", "NXDOMAIN", "REFUSED").</param>
/// <param name="DurationMs">The total processing duration for the DNS query in milliseconds.</param>
/// <param name="Blocked">Indicates whether the DNS request was blocked by a rule or sinkhole.</param>
/// <param name="Upstream">The IP address or domain of the upstream DNS server that resolved the query, if applicable.</param>
/// <param name="AnswerData">The resolved DNS payload records represented as a list of strings (e.g., IPs, CNAMEs).</param>
/// <param name="TtlSeconds">The Time-To-Live duration in seconds returned for the DNS answer record, if available.</param>
/// <param name="BlockRuleId">The identifier or name of the filter rule/list that triggered a block action, if applicable.</param>
/// <param name="BlockRulePattern">The Pattern that the domain matched</param>
/// <param name="HeuristicScore">Heuristtic Score </param>
public sealed record DnsResponseEventDto(
    string Id,
    DateTimeOffset StartTimeUtc,
    string ContextId,
    string QuestionName,
    string QuestionType,
    string ClientAddress,
    string? ClientName,
    string ResolutionSource,
    string Rcode,
    double DurationMs,
    bool Blocked,
    string? Upstream,
    IReadOnlyList<string>? AnswerData,
    int? TtlSeconds,
    int? BlockRuleId,
    string? BlockRulePattern,
    double HeuristicScore
);
