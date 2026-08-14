using System;
using System.Net;

namespace Astrolabed.Api.Services;

public sealed record NtpResponse
{
    public required bool Success { get; init; }
    public required string Server { get; init; }
    public required DateTimeOffset SystemTimeUtc { get; init; }
    public required DateTimeOffset NetworkTimeUtc { get; init; }
    public required TimeSpan Offset { get; init; }
    public required TimeSpan Delay { get; init; }
    public required NtpPacketHeader Header { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed record NtpPacketHeader
{
    public required int LeapIndicator { get; init; }
    public required int Version { get; init; }
    public required int Mode { get; init; }
    public required int Stratum { get; init; }
    public required int PollInterval { get; init; }
    public required double PrecisionSeconds { get; init; }
    public required double RootDelayMs { get; init; }
    public required double RootDispersionMs { get; init; }
    public required string ReferenceId { get; init; }
    public required DateTimeOffset ReferenceTimestamp { get; init; }
    public required DateTimeOffset OriginateTimestamp { get; init; }
    public required DateTimeOffset ReceiveTimestamp { get; init; }
    public required DateTimeOffset TransmitTimestamp { get; init; }
}
