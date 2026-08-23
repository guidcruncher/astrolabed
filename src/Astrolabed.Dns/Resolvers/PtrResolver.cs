// File: src/Astrolabed.Dns/Resolvers/PtrResolver.cs
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Globalization;
using System.Net;

using Astrolabed.Dns.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Resolvers;

/// <summary>
/// Resolves reverse DNS pointer (PTR) record queries and conditional subnets using zero-allocation span parsing.
/// </summary>
/// <param name="optionsMonitor">Options monitor tracking DNS engine settings.</param>
/// <param name="logger">Structured logger instance.</param>
public sealed partial class PtrResolver : IPtrResolver, IDisposable
{
    private static readonly IdnMapping IdnMapping = new();

    private readonly IOptionsMonitor<DnsEngineOptions> _optionsMonitor;
    private readonly ILogger<PtrResolver> _logger;
    private readonly IDisposable? _optionsChangeListener;

    private FrozenDictionary<IPAddress, string> _ipToPtrMap = FrozenDictionary<IPAddress, string>.Empty;
    private ConditionalRulesHolder _conditionalRules = ConditionalRulesHolder.Empty;

    public PtrResolver(
        IOptionsMonitor<DnsEngineOptions> optionsMonitor,
        ILogger<PtrResolver> logger)
    {
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        RebuildTables(_optionsMonitor.CurrentValue);
        _optionsChangeListener = _optionsMonitor.OnChange(RebuildTables);
    }

    /// <inheritdoc />
    public bool TryResolvePtr(string ptrQuery, out string? domainName)
    {
        domainName = null;

        if (string.IsNullOrWhiteSpace(ptrQuery))
        {
            return false;
        }

        if (!TryParsePtrQueryToIp(ptrQuery, out IPAddress? parsedIp) || parsedIp is null)
        {
            return false;
        }

        // Direct Static Record Match via Volatile Read
        FrozenDictionary<IPAddress, string> map = Volatile.Read(ref _ipToPtrMap);
        return map.TryGetValue(parsedIp, out domainName);
    }

    /// <summary>
    /// Attempts to resolve a conditional forwarder target address for a reverse DNS PTR query.
    /// </summary>
    /// <param name="ptrQuery">The PTR query string to inspect.</param>
    /// <param name="targetResolver">Outputs the designated forwarder IP address if matched; otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if a matching subnet rule was found; otherwise <c>false</c>.</returns>
    public bool TryGetConditionalForwarder(string ptrQuery, out IPAddress? targetResolver)
    {
        targetResolver = null;

        if (string.IsNullOrWhiteSpace(ptrQuery))
        {
            return false;
        }

        if (!TryParsePtrQueryToIp(ptrQuery, out IPAddress? parsedIp) || parsedIp is null)
        {
            return false;
        }

        ConditionalRulesHolder holder = Volatile.Read(ref _conditionalRules);
        ImmutableArray<(IPNetwork Network, IPAddress TargetServer)> rules = holder.Rules;
        for (int i = 0; i < rules.Length; i++)
        {
            var (network, server) = rules[i];
            if (network.Contains(parsedIp))
            {
                targetResolver = server;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Parses an IPv4 or IPv6 PTR query string into a concrete <see cref="IPAddress"/>.
    /// </summary>
    /// <param name="ptrQuery">The PTR query string (e.g. "1.0.0.127.in-addr.arpa").</param>
    /// <param name="ipAddress">Outputs the parsed IP address if successful.</param>
    /// <returns><c>true</c> if successfully parsed; otherwise <c>false</c>.</returns>
    public static bool TryParsePtrQueryToIp(string ptrQuery, out IPAddress? ipAddress)
    {
        ipAddress = null;
        if (string.IsNullOrWhiteSpace(ptrQuery))
        {
            return false;
        }

        ReadOnlySpan<char> query = ptrQuery.AsSpan().Trim().TrimEnd('.');

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

    private static bool TryParseIPv4Ptr(ReadOnlySpan<char> query, out IPAddress? ipAddress)
    {
        ipAddress = null;
        ReadOnlySpan<char> labels = query[..^".in-addr.arpa".Length];
        if (labels.IsEmpty)
        {
            return false;
        }

        Span<byte> octets = stackalloc byte[4];
        int octetCount = 0;

        foreach (Range range in labels.Split('.'))
        {
            if (octetCount >= 4)
            {
                return false;
            }

            ReadOnlySpan<char> label = labels[range];

            if (!byte.TryParse(label, NumberStyles.None, CultureInfo.InvariantCulture, out byte octet))
            {
                return false;
            }

            if (label.Length > 1 && label[0] == '0')
            {
                return false;
            }

            octets[octetCount++] = octet;
        }

        if (octetCount != 4)
        {
            return false;
        }

        Span<byte> ipBytes = stackalloc byte[4] { octets[3], octets[2], octets[1], octets[0] };
        ipAddress = new IPAddress(ipBytes);
        return true;
    }

    private static bool TryParseIPv6Ptr(ReadOnlySpan<char> query, out IPAddress? ipAddress)
    {
        ipAddress = null;
        ReadOnlySpan<char> labels = query[..^".ip6.arpa".Length];
        if (labels.IsEmpty)
        {
            return false;
        }

        Span<byte> nibbles = stackalloc byte[32];
        int nibbleCount = 0;

        foreach (Range range in labels.Split('.'))
        {
            if (nibbleCount >= 32)
            {
                return false;
            }

            ReadOnlySpan<char> label = labels[range];
            if (label.Length != 1)
            {
                return false;
            }

            char ch = label[0];
            int value = ch switch
            {
                >= '0' and <= '9' => ch - '0',
                >= 'a' and <= 'f' => ch - 'a' + 10,
                >= 'A' and <= 'F' => ch - 'A' + 10,
                _ => -1
            };

            if (value == -1)
            {
                return false;
            }

            nibbles[nibbleCount++] = (byte)value;
        }

        if (nibbleCount != 32)
        {
            return false;
        }

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
        var newMap = new Dictionary<IPAddress, string>();

        foreach (var (key, domainName) in options.PtrRecords)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(domainName))
            {
                continue;
            }

            string canonicalDomain;
            try
            {
                canonicalDomain = IdnMapping.GetAscii(domainName.Trim().TrimEnd('.')).ToLowerInvariant();
            }
            catch (ArgumentException)
            {
                LogInvalidDomainInPtrOptions(_logger, domainName);
                continue;
            }

            if (TryParsePtrQueryToIp(key, out IPAddress? ipFromPtr) && ipFromPtr is not null)
            {
                newMap[ipFromPtr] = canonicalDomain;
            }
            else if (IPAddress.TryParse(key, out IPAddress? directIp))
            {
                newMap[directIp] = canonicalDomain;
            }
        }

        var newRules = ImmutableArray.CreateBuilder<(IPNetwork Network, IPAddress TargetServer)>();
        foreach (PtrConditionalRule rule in options.ConditionalPtrRules)
        {
            if (IPNetwork.TryParse(rule.Subnet, out IPNetwork network) && IPAddress.TryParse(rule.TargetResolver, out IPAddress? server))
            {
                newRules.Add((network, server));
            }
            else
            {
                LogInvalidConditionalPtrRule(_logger, rule.Subnet, rule.TargetResolver);
            }
        }

        Volatile.Write(ref _ipToPtrMap, newMap.ToFrozenDictionary());
        Volatile.Write(ref _conditionalRules, new ConditionalRulesHolder(newRules.ToImmutable()));

        LogPtrTableUpdated(_logger, newMap.Count, newRules.Count);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _optionsChangeListener?.Dispose();
    }

    private sealed class ConditionalRulesHolder(ImmutableArray<(IPNetwork Network, IPAddress TargetServer)> rules)
    {
        public static readonly ConditionalRulesHolder Empty = new(ImmutableArray<(IPNetwork, IPAddress)>.Empty);

        public ImmutableArray<(IPNetwork Network, IPAddress TargetServer)> Rules { get; } = rules;
    }

    [LoggerMessage(
        EventId = 601,
        Level = LogLevel.Warning,
        Message = "Invalid domain format in PTR options: {Domain}")]
    private static partial void LogInvalidDomainInPtrOptions(ILogger logger, string domain);

    [LoggerMessage(
        EventId = 602,
        Level = LogLevel.Warning,
        Message = "Invalid conditional PTR rule: Subnet={Subnet}, Server={Server}")]
    private static partial void LogInvalidConditionalPtrRule(ILogger logger, string subnet, string server);

    [LoggerMessage(
        EventId = 603,
        Level = LogLevel.Information,
        Message = "PTR Table updated. Static entries: {StaticCount}, Conditional rules: {RuleCount}")]
    private static partial void LogPtrTableUpdated(ILogger logger, int staticCount, int ruleCount);
}
