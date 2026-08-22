using System.Net;

namespace Astrolabed.EventBus.Events;

public sealed record DnsResponseEvent(
DateTimeOffset StartTimeUTC,
string ContextId,
string QuestionName,
string QuestionType,
EndPoint ClientEndpoint,
string ClientName,
string ResolutionSource,
double DurationMs
);
