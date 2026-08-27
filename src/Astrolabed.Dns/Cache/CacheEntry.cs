using Astrolabed.Dns.Models;

namespace Astrolabed.Dns.Cache;

/// <summary>
/// Represents an immutable, thread-safe binary DNS cache entry containing raw payload bytes and an expiration timestamp.
/// </summary>
/// <param name="Payload">The binary DNS response payload bytes.</param>
/// <param name="ExpiresAt">The UTC timestamp when this cached entry expires.</param>
public sealed record CacheEntry(ReadOnlyMemory<byte> Payload, DateTimeOffset ExpiresAt)
{
    /// <summary>
    /// Gets a value indicating whether the cache entry has expired relative to <see cref="DateTimeOffset.UtcNow"/>.
    /// </summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
}

/// <summary>
/// Represents an immutable, thread-safe view of a DNS cache entry containing a parsed wire message and an expiration timestamp.
/// </summary>
/// <param name="Payload">The parsed <see cref="DnsWireMessage"/> payload instance.</param>
/// <param name="ExpiresAt">The UTC timestamp when this cached entry expires.</param>
public sealed record CacheEntryView(DnsWireMessage Payload, DateTimeOffset ExpiresAt)
{
    /// <summary>
    /// Gets a value indicating whether the cache entry view has expired relative to <see cref="DateTimeOffset.UtcNow"/>.
    /// </summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
}
