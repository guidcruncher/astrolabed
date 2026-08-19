// File: src/Astrolabed.Dns/Cache/DnsCache.cs
using System;
using System.Collections.Concurrent;
using Astrolabed.Dns.Options;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Cache;

public sealed class DnsCache : IDnsCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new();
    private readonly IOptionsMonitor<DnsEngineOptions> _optionsMonitor;

    public LockFreeDnsCache(IOptionsMonitor<DnsEngineOptions> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;
    }

    public bool TryGet(string domain, ushort qType, out byte[]? payload)
    {
        string key = $"{domain}:{qType}";
        if (_entries.TryGetValue(key, out var entry))
        {
            if (entry.ExpiresAt > DateTimeOffset.UtcNow)
            {
                payload = entry.Payload;
                return true;
            }

            _entries.TryRemove(key, out _);
        }

        payload = null;
        return false;
    }

    public void Store(string domain, ushort qType, byte[] payload, TimeSpan ttl)
    {
        if (_entries.Count >= _optionsMonitor.CurrentValue.MaxCacheEntries)
        {
            PurgeExpired();
        }

        string key = $"{domain}:{qType}";
        _entries[key] = new CacheEntry(payload, DateTimeOffset.UtcNow.Add(ttl));
    }

    private void PurgeExpired()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var (key, entry) in _entries)
        {
            if (entry.ExpiresAt <= now)
            {
                _entries.TryRemove(key, out _);
            }
        }
    }
}
