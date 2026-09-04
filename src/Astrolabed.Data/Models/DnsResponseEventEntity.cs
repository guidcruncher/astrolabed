namespace Astrolabed.Data.Models;

/// <summary>
/// Represents the database record layout stored in the 'dns_response_events' table.
/// </summary>
public sealed class DnsResponseEventEntity
{
    /// <summary>
    /// Gets or sets the unique primary key identifier string of the log record.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the query execution start time represented as a UTC Unix epoch timestamp in milliseconds.
    /// </summary>
    public long StartTimeUtc { get; set; }

    /// <summary>
    /// Gets or sets the unique context tracking identifier string associated with the query request.
    /// </summary>
    public string ContextId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the queried domain name string (e.g., "example.com").
    /// </summary>
    public string QuestionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the queried DNS record type string (e.g., "A", "AAAA", "HTTPS").
    /// </summary>
    public string QuestionType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client Address IP and port string representation (e.g., "192.168.1.100:54321").
    /// </summary>
    public string ClientAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the resolved name of the client making the DNS request, or <see langword="null"/> if unresolvable.
    /// </summary>
    public string? ClientName { get; set; }

    /// <summary>
    /// Gets or sets the resolution outcome source description tag (e.g., "CACHE", "UPSTREAM", "BLOCKED").
    /// </summary>
    public string ResolutionSource { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the standard DNS response code (e.g., "NOERROR", "NXDOMAIN", "REFUSED").
    /// </summary>
    public string Rcode { get; set; } = "UNKNOWN";

    /// <summary>
    /// Gets or sets the total processing duration for the DNS query in milliseconds.
    /// </summary>
    public double DurationMs { get; set; }

    /// <summary>
    /// Gets or sets an integer value indicating if this response was blocked (1 = Blocked, 0 = Allowed).
    /// Compatible across both PostgreSQL and SQLite backends.
    /// </summary>
    public int Blocked { get; set; }

    /// <summary>
    /// Gets or sets the resolving upstream server, or <see langword="null"/> if resolved via cache or blocked.
    /// </summary>
    public string? Upstream { get; set; }

    /// <summary>
    /// Gets or sets the JSON-serialized array string representing resolved DNS answer records, or <see langword="null"/>.
    /// </summary>
    public string? AnswerDataJson { get; set; }

    /// <summary>
    /// Gets or sets the record Time-To-Live (TTL) in seconds returned by the DNS response, or <see langword="null"/>.
    /// </summary>
    public int? TtlSeconds { get; set; }

    /// <summary>
    /// Gets or sets the name or identifier string of the specific block rule list that matched, if blocked.
    /// </summary>
    public int? BlockRuleId { get; set; }

    /// <summary>
    /// The block rule pattern match
    /// </summary>
    public string? BlockRulePattern { get; set; }

    /// <summary>
    /// Heuristic Score
    /// </summary>
    public double HeuristicScore { get; set; }
}
