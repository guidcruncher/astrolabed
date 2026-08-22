namespace Astrolabed.Data.Models;

/// <summary>
/// Represents the database record layout stored in the 'dns_response_events' table.
/// </summary>
public sealed class DnsResponseEventEntity
{
    public string Id { get; set; } = string.Empty;

    public long StartTimeUtc { get; set; }

    public string ContextId { get; set; } = string.Empty;

    public string QuestionName { get; set; } = string.Empty;

    public string QuestionType { get; set; } = string.Empty;

    public string ClientEndpoint { get; set; } = string.Empty;

    public string ClientName { get; set; } = string.Empty;

    public string ResolutionSource { get; set; } = string.Empty;

    public double DurationMs { get; set; }
}
