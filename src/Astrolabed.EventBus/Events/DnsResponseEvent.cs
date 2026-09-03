namespace Astrolabed.EventBus.Events;

/// <summary>
/// Domain telemetry event dispatched upon completing a DNS request resolution.
/// </summary>
/// <param name="StartTimeUTC">The UTC timestamp when query resolution began.</param>
/// <param name="ContextId">The trace context GUID string identifying the request operation.</param>
/// <param name="QuestionName">The requested domain name string.</param>
/// <param name="QuestionType">The requested DNS record type string.</param>
/// <param name="ClientAddress">The network Address of the asking client.</param>
/// <param name="ClientName">The resolved network hostname or IP representation of the client, if available.</param>
/// <param name="ResolutionSource">The resolution source tag describing how the query was answered (e.g., "CACHE", "UPSTREAM", "BLOCKLIST").</param>
/// <param name="Rcode">The standard DNS response code (e.g., "NOERROR", "NXDOMAIN", "REFUSED").</param>
/// <param name="DurationMs">The overall duration taken to resolve the query in milliseconds.</param>
/// <param name="Blocked">Indicates whether the response was blocked by a rule or sinkhole.</param>
/// <param name="Upstream">Indicates the resolving upstream server, or <see langword="null"/> if resolved locally.</param>
/// <param name="AnswerData">The collection of resolved answer payloads (e.g., IP addresses, CNAME records), if present.</param>
/// <param name="TtlSeconds">The minimum or average Time-To-Live duration in seconds for the answer record, if available.</param>
/// <param name="BlockRuleId">The identifier or tag of the specific filter rule/list that triggered a block action, if blocked.</param>
/// <param name="BlockRulePattern">The matching pattern</param>
public sealed record DnsResponseEvent(
    DateTimeOffset StartTimeUTC,
    string ContextId,
    string QuestionName,
    string QuestionType,
    string ClientAddress,
    string? ClientName,
    string ResolutionSource,
    string Rcode,
    double DurationMs,
    bool Blocked,
    string? Upstream = null,
    IReadOnlyList<string>? AnswerData = null,
    int? TtlSeconds = null,
    int? BlockRuleId = null,
    string? BlockRulePattern = null
);
