using System.Net;
using System.Net.Sockets;

using Astrolabed.Dns.Models;

namespace Astrolabed.Dns.Events;

public sealed record DnsResponseEvent(
DateTimeOffset StartTimeUTC,
string ContextId,
string QuestionName,
DnsType QuestionType,
EndPoint ClientEndpoint,
string ResolutionSource,
double DurationMs
);
