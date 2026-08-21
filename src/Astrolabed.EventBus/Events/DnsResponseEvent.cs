using System.Net;
using System.Net.Sockets;

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
