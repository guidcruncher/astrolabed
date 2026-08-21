namespace Astrolabed.Data.Models;


/// <summary>
/// Data Transfer Object representing a persisted DNS response event.
/// </summary>
public sealed record DnsResponseEventDto(
    string Id,
    DateTimeOffset StartTimeUtc,
    string ContextId,
    string QuestionName,
    string QuestionType,
    string ClientEndpoint,
    string ClientName,
    string ResolutionSource,
    double DurationMs
);
