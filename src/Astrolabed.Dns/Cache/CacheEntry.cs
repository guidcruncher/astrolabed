namespace Astrolabed.Dns.Cache;

/// <summary>
/// Represents an immutable, thread-safe DNS cache entry payload and expiration timestamp.
/// </summary>
/// <param name="Payload">The binary DNS response payload bytes.</param>
/// <param name="ExpiresAt">The UTC timestamp when this cached entry expires.</param>
public sealed record CacheEntry(ReadOnlyMemory<byte> Payload, DateTimeOffset ExpiresAt)
{
    /// <summary>
    /// Gets a value indicating whether the cache entry has expired relative to UTC now.
    /// </summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
}
