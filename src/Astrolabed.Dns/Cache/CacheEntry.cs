// File: src/Astrolabed.Dns/Cache/CacheEntry.cs
using System;

namespace Astrolabed.Dns.Cache;

public sealed record CacheEntry(byte[] Payload, DateTimeOffset ExpiresAt);
