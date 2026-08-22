// File: src/Astrolabed.Dns/Cache/CacheEntry.cs
namespace Astrolabed.Dns.Cache;

public sealed record CacheEntry(byte[] Payload, DateTimeOffset ExpiresAt);
