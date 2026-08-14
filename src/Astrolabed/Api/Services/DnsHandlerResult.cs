using System.Collections.Generic;

namespace Astrolabed.Api.Services;

public sealed record DnsHandlerResult
{
    public required bool Success { get; init; }
    public required string? ResponseCode { get; init; }
    public required byte[] Bytes { get; init; }
    public required IReadOnlyList<DnsResourceRecord> Answers { get; init; }
    public required IReadOnlyList<DnsResourceRecord> Authorities { get; init; }
    public required IReadOnlyList<DnsResourceRecord> Additionals { get; init; }
}
