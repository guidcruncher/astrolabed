// File: src/Astrolabed.Dns/Resolvers/PtrResolver.cs
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;

using Astrolabed.Dns.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Resolvers;

public sealed class PtrResolver : IPtrResolver
{
    private static readonly IdnMapping IdnMapping = new();
    private readonly IOptionsMonitor<DnsEngineOptions> _optionsMonitor;
    private readonly ILogger<PtrResolver> _logger;
    private readonly IDisposable? _optionsChangeListener;

    private ConcurrentDictionary<IPAddress, string> _ipToPtrMap = new();
    private List<(IPNetwork Network, IPAddress TargetServer)> _conditionalRules = new();

    public PtrResolver(
        IOptionsMonitor<DnsEngineOptions> optionsMonitor,
        ILogger<PtrResolver> logger)
    {
        _optionsMonitor = optionsMonitor;
        _logger = logger;

        RebuildTables(_optionsMonitor.CurrentValue);
        _optionsChangeListener = _optionsMonitor.OnChange(RebuildTables);
    }

    public bool TryResolvePtr(string ptrQuery, out string? domainName)
    {
        domainName = null;

        if (string.IsNullOrWhiteSpace(ptrQuery)) return false;
        if (!TryParsePtrQueryToIp(ptrQuery, out var parsedIp) || parsedIp == null) return false;

        // 1. Direct Static Record Match
        if (_ipToPtrMap.TryGetValue(parsedIp, out var matchedDomain))
        {
            domainName = matchedDomain;
            return true;
        }

        return false;
    }

    public bool TryGetConditionalForwarder(string ptrQuery, out IPAddress? targetResolver)
    {
        targetResolver = null;

        if (string.IsNullOrWhiteSpace(ptrQuery)) return false;
        if (!TryParsePtrQueryToIp(ptrQuery, out var parsedIp) || parsedIp == null) return false;

        // 2. Subnet Match for Conditional Forwarding
        foreach (var (network, server) in _conditionalRules)
        {
            if (network.Contains(parsedIp))
            {
                targetResolver = server;
                return true;
            }
        }

        return false;
    }

    public static bool TryParsePtrQueryToIp(string ptrQuery, out IPAddress? ipAddress)
    {
        ipAddress = null;
        string query = ptrQuery.Trim().TrimEnd('.');

        if (query.EndsWith(".in-addr.arpa", StringComparison.OrdinalIgnoreCase))
        {
            return TryParseIPv4Ptr(query, out ipAddress);
        }

        if (query.EndsWith(".ip6.arpa", StringComparison.OrdinalIgnoreCase))
        {
            return TryParseIPv6Ptr(query, out ipAddress);
        }

        return false;
    }

    private static bool TryParseIPv4Ptr(string query, out IPAddress? ipAddress)
    {
        ipAddress = null;
        ReadOnlySpan<char> labels = query.AsSpan(0, query.Length - ".in-addr.arpa".Length);
        if (labels.IsEmpty) return false;

        Span<byte> octets = stackalloc byte[4];
        int octetCount = 0;

        foreach (var range in labels.Split('.'))
        {
            if (octetCount >= 4) return false;
            ReadOnlySpan<char> label = labels[range];

            if (!byte.TryParse(label, NumberStyles.None, CultureInfo.InvariantCulture, out byte octet)) return false;
            if (label.Length > 1 && label[0] == '0') return false;

            octets[octetCount++] = octet;
        }

        if (octetCount != 4) return false;

        Span<byte> ipBytes = stackalloc byte[4] { octets[3], octets[2], octets[1], octets[0] };
        ipAddress = new IPAddress(ipBytes);
        return true;
    }

    private static bool TryParseIPv6Ptr(string query, out IPAddress? ipAddress)
    {
        ipAddress = null;
        ReadOnlySpan<char> labels = query.AsSpan(0, query.Length - ".ip6.arpa".Length);
        if (labels.IsEmpty) return false;

        Span<byte> nibbles = stackalloc byte[32];
        int nibbleCount = 0;

        foreach (var range in labels.Split('.'))
        {
            if (nibbleCount >= 32) return false;
            ReadOnlySpan<char> label = labels[range];
            if (label.Length != 1) return false;

            char ch = label[0];
            int value = ch switch
            {
                >= '0' and <= '9' => ch - '0',
                >= 'a' and <= 'f' => ch - 'a' + 10,
                >= 'A' and <= 'F' => ch - 'A' + 10,
                _ => -1
            };

            if (value == -1) return false;
            nibbles[nibbleCount++] = (byte)value;
        }

        if (nibbleCount != 32) return false;

        Span<byte> ipBytes = stackalloc byte[16];
        for (int i = 0; i < 16; i++)
        {
            byte high = nibbles[31 - (i * 2 + 1)];
            byte low = nibbles[31 - (i * 2)];
            ipBytes[i] = (byte)((high << 4) | low);
        }

        ipAddress = new IPAddress(ipBytes);
        return true;
    }

    private void RebuildTables(DnsEngineOptions options)
    {
        var newMap = new ConcurrentDictionary<IPAddress, string>();

        foreach (var (key, domainName) in options.PtrRecords)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(domainName)) continue;

            string canonicalDomain;
            try
            {
                canonicalDomain = IdnMapping.GetAscii(domainName.Trim().TrimEnd('.')).ToLowerInvariant();
            }
            catch (ArgumentException)
            {
                _logger.LogWarning("Invalid domain format in PTR options: {Domain}", domainName);
                continue;
            }

            if (TryParsePtrQueryToIp(key, out var ipFromPtr) && ipFromPtr != null)
            {
                newMap[ipFromPtr] = canonicalDomain;
            }
            else if (IPAddress.TryParse(key, out var directIp))
            {
                newMap[directIp] = canonicalDomain;
            }
        }

        var newRules = new List<(IPNetwork Network, IPAddress TargetServer)>();
        foreach (var rule in options.ConditionalPtrRules)
        {
            if (IPNetwork.TryParse(rule.Subnet, out var network) && IPAddress.TryParse(rule.TargetResolver, out var server))
            {
                newRules.Add((network, server));
            }
            else
            {
                _logger.LogWarning("Invalid conditional PTR rule: Subnet={Subnet}, Server={Server}", rule.Subnet, rule.TargetResolver);
            }
        }

        _ipToPtrMap = newMap;
        _conditionalRules = newRules;
        _logger.LogInformation("PTR Table updated. Static entries: {StaticCount}, Conditional rules: {RuleCount}", newMap.Count, newRules.Count);
    }
}

