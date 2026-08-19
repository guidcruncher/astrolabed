// File: src/Astrolabed.Dns/CacheIDnsCache.cs
using System;

namespace Astrolabed.Dns.Cache;

public interface IDnsCache
{
    bool TryGet(string domain, ushort qType, out byte[]? payload);
    void Store(string domain, ushort qType, byte[] payload, TimeSpan ttl);
}
