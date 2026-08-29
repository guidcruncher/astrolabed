using System.Net;

namespace Astrolabed.EventBus.Events;

/// <summary>
/// Domain telemetry event dispatched upon completing a DNS request resolution.
/// </summary>
/// <param name="StartTimeUTC">The UTC timestamp when query resolution began.</param>
/// <param name="ContextId">The trace context GUID string identifying the request operation.</param>
/// <param name="QuestionName">The requested domain name string.</param>
/// <param name="QuestionType">The requested DNS record type string.</param>
/// <param name="ClientEndpoint">The network endpoint of the asking client.</param>
/// <param name="ClientName">The resolved network hostname or IP representation of the client.</param>
/// <param name="ResolutionSource">The resolution source tag describing how the query was answered.</param>
/// <param name="DurationMs">The overall duration taken to resolve the query in milliseconds.</param>
/// <param name="Blocked">Indicates if the response was blocked.</param>
/// <param name="Upstream">Indicate the resolving upstream.</param>
public sealed record DnsResponseEvent(
    DateTimeOffset StartTimeUTC,
    string ContextId,
    string QuestionName,
    string QuestionType,
    EndPoint ClientEndpoint,
    string ClientName,
    string ResolutionSource,
    double DurationMs,
    bool Blocked,
    string Upstream
);
