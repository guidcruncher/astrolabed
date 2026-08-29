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
    /// Gets or sets the query execution start time represented as a UTC Unix epoch timestamp in seconds.
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
    /// Gets or sets the queried DNS record type string (e.g., "A", "AAAA", "PTR").
    /// </summary>
    public string QuestionType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client endpoint IP and port string representation.
    /// </summary>
    public string ClientEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the resolved name of the client making the DNS request.
    /// </summary>
    public string ClientName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the resolution outcome source description tag (e.g., "CACHE", "HOSTS_FILE", "UPSTREAM").
    /// </summary>
    public string ResolutionSource { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total processing duration for the DNS query in milliseconds.
    /// </summary>
    public double DurationMs { get; set; }

    /// <summary>
    /// Gets or sets a value to indicate if this response was blocked
    /// </summary>
    public int Blocked { get; set; }

    /// <summary>
    /// Gets or sets a value to indicate the resolving upstream
    /// </summary>
    public string Upstream { get; set; }

}
