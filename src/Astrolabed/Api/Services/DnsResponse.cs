using System;
using System.Collections.Generic;

namespace Astrolabed.Api.Services;

public sealed record DnsResponse
{
    public required bool Success { get; init; }
    public required string Server { get; init; }
    public required string QueryName { get; init; }
    public required string QueryType { get; init; }
    public required string ResponseCode { get; init; }
    public required TimeSpan Elapsed { get; init; }
    public required DnsHeader Header { get; init; }
    public required IReadOnlyList<DnsResourceRecord> Answers { get; init; }
    public required IReadOnlyList<DnsResourceRecord> Authorities { get; init; }
    public required IReadOnlyList<DnsResourceRecord> Additionals { get; init; }
    public DnsExtendedError? ExtendedError { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed record DnsHeader
{
    public required ushort TransactionId { get; init; }
    public required bool IsResponse { get; init; }
    public required string OpCode { get; init; }
    public required bool AuthoritativeAnswer { get; init; }
    public required bool Truncated { get; init; }
    public required bool RecursionDesired { get; init; }
    public required bool RecursionAvailable { get; init; }
    public required bool AuthenticData { get; init; }
    public required bool CheckingDisabled { get; init; }
    public required ushort QuestionCount { get; init; }
    public required ushort AnswerCount { get; init; }
    public required ushort NameServerCount { get; init; }
    public required ushort AdditionalCount { get; init; }
    public DnsExtendedError? ExtendedError { get; init; }
}

public sealed record DnsExtendedError
{
    public required ushort Code { get; init; }
    public required string Name { get; init; }
    public string? ExtraText { get; init; }
}

public sealed record DnsResourceRecord
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public required string Class { get; init; }
    public required uint TimeToLive { get; init; }
    public required string Data { get; init; }
}
