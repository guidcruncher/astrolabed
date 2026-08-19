using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Dns.Core;
using Astrolabed.Dns.Filtering;
using Astrolabed.Events;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.RuleEngine;

public sealed class RuleEngine : IDisposable
{
    private readonly ILogger<RuleEngine> _logger;
    private readonly DnsForwarderOptions _options;

    private readonly IDnsMetrics _metrics;
    private readonly RuleCompiler _compiler;
    private readonly RuleMatcher _matcher;
    private readonly ResolverChainBuilder _chainBuilder;
    private readonly QueryExecutor _executor;
    private bool _disposed;

    private const ushort EdeCodeBlocked = 15;
    private const ushort EdeCodeFiltered = 17;

    public IDnsCache Cache { get; }
    public bool BlockAll { get; private set; }

    public RuleEngine(
        IOptions<DnsForwarderOptions> options,
        ILogger<RuleEngine> logger,
        IDnsClientFactory clientFactory,
        IDnsCache dnsCache,
        IDnsMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(clientFactory);

        _options = options.Value;
        _logger = logger;
        _metrics = metrics;

        BlockAll = false;
        Cache = dnsCache;

        _compiler = new RuleCompiler(options, logger, clientFactory);
        _matcher = new RuleMatcher(_compiler, logger);
        _chainBuilder = new ResolverChainBuilder(options, _compiler.DefaultClient, _compiler.FallbackResolvers, clientFactory, logger);
        _executor = new QueryExecutor(Cache, options, logger);

        if (_options.Resolvers != null)
        {
            foreach (var r in _options.Resolvers)
            {
                _compiler.AddResolver(r);
            }
        }

        _compiler.BuildAutomata();
    }

    public bool setBlockAll(bool state)
    {
        BlockAll = state;
        return BlockAll;
    }

    public async Task AddHostsAsync(IHostsFileSource src)
    {
        var entries = await src.LoadAsync().ConfigureAwait(false);

        foreach (var h in entries)
        {
            _compiler.Hosts.Add(h.Domain, h.Address);
        }
    }

    public async Task AddListAsync(IBlocklistSource source, bool block)
    {
        var parsed = await source.LoadAsync().ConfigureAwait(false);
        _compiler.AddRules(parsed, block);
        _compiler.BuildAutomata();
    }

    public Task<byte[]> QueryAsync(in DnsRequestContext context, CancellationToken ct)
    {
        var match = Match(context.Domain, context.RequestId);
        return QueryAsync(context, match, ct);
    }

    public async Task<byte[]> QueryAsync(DnsRequestContext context, RuleResult match, CancellationToken ct)
    {
        bool isDebug = _logger.IsEnabled(LogLevel.Debug);

        if (BlockAll)
        {
            if (isDebug)
            {
                _logger.LogDebug("Request {RequestId}: Globally Blocked {Domain} using mode {Mode}",
                    context.RequestId, context.Domain, _options.BlockResponse?.Mode);
            }
            return BuildBlockResponse(context.RawRequest);
        }

        if (Cache.TryGet(context, out var cached) && cached != null)
        {
            if (isDebug)
            {
                _logger.LogDebug("Request {RequestId}: Cache HIT for {Domain}", context.RequestId, context.Domain);
            }

            return cached;
        }

        if (isDebug)
        {
            _logger.LogDebug("Request {RequestId}: Cache MISS for {Domain}", context.RequestId, context.Domain);
        }

        if (match.Block)
        {
            if (isDebug)
            {
                _logger.LogDebug("Request {RequestId}: Blocked {Domain} using mode {Mode}",
                    context.RequestId, context.Domain, _options.BlockResponse?.Mode);
            }

            _metrics.RecordDnsResponse(new DnsResponseEvent(
                                 Timestamp: DateTimeOffset.UtcNow,
                                 TimestampEpoch: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                                 IsBlocked: true,
                                 ClientIp: System.Net.IPAddress.Parse(context.ClientIp),
                                 ClientName: context.ClientName,
                                 QueryName: context.Domain,
                                 QueryType: Enum.GetName(typeof(DnsType), context.QType) ?? "UNKNOWN",
                                 Status: DnsResponseCode.Refused,
                                 ResponseIp: null));
            return BuildBlockResponse(context.RawRequest);
        }

        var upstreams = _chainBuilder.BuildChain(match, context.Domain, context.RequestId);

        var response = await _executor.ExecuteAsync(upstreams, context, ct).ConfigureAwait(false);

        if (response == null)
        {
            _logger.LogError("Request {RequestId}: All upstreams failed for {Domain}, returning SERVFAIL",
                context.RequestId, context.Domain);

            return DnsResponseBuilder.BuildServfail(context.RawRequest);
        }

        return response;
    }

    public RuleResult Match(string domain, string? requestId)
    {
        return _matcher.Match(domain, requestId);
    }

    public void AddRules(IEnumerable<ParsedRule> rules, bool block)
    {
        _compiler.AddRules(rules, block);
        _compiler.BuildAutomata();
    }

    private byte[] BuildBlockResponse(byte[] rawRequest, ReadOnlySpan<byte> upstreamEdeBytes = default)
    {
        if (rawRequest == null || rawRequest.Length < 12)
        {
            return DnsResponseBuilder.BuildServfail(rawRequest ?? Array.Empty<byte>());
        }

        if (!upstreamEdeBytes.IsEmpty)
        {
            byte[] baseResp = DnsResponseBuilder.BuildRcodeResponse(rawRequest, 2);
            return DnsResponseBuilder.AttachUpstreamEde(baseResp, upstreamEdeBytes);
        }

        string mode = _options.BlockResponse?.Mode ?? "NxDomain";

        return mode.ToLowerInvariant() switch
        {
            "nxdomain" => DnsResponseBuilder.BuildRcodeResponseWithEde(rawRequest, 3, EdeCodeBlocked),
            "refused" => DnsResponseBuilder.BuildRcodeResponseWithEde(rawRequest, 5, EdeCodeBlocked),
            "filtered" => DnsResponseBuilder.BuildRcodeResponseWithEde(rawRequest, 5, EdeCodeFiltered),
            "nodata" => DnsResponseBuilder.BuildRcodeResponseWithEde(rawRequest, 0, EdeCodeBlocked),
            "servfail" => DnsResponseBuilder.BuildServfail(rawRequest),
            "zeroip" => BuildZeroIpResponse(rawRequest),
            "customip" => BuildCustomIpResponse(rawRequest),
            _ => DnsResponseBuilder.BuildRcodeResponseWithEde(rawRequest, 3, EdeCodeBlocked)
        };
    }

    private static byte[] BuildZeroIpResponse(byte[] rawRequest)
    {
        var msg = DnsMessage.TryParse(rawRequest);
        if (msg == null)
        {
            return DnsResponseBuilder.BuildRcodeResponseWithEde(rawRequest, 3, EdeCodeBlocked);
        }

        string qType = msg.QuestionType;

        if (string.Equals(qType, "A", StringComparison.OrdinalIgnoreCase) || qType == "1")
        {
            return DnsResponseBuilder.BuildStaticIpResponse(rawRequest, IPAddress.Any);
        }

        if (string.Equals(qType, "AAAA", StringComparison.OrdinalIgnoreCase) || qType == "28")
        {
            return DnsResponseBuilder.BuildStaticIpResponse(rawRequest, IPAddress.IPv6Any);
        }

        return DnsResponseBuilder.BuildRcodeResponseWithEde(rawRequest, 3, EdeCodeBlocked);
    }

    private byte[] BuildCustomIpResponse(byte[] rawRequest)
    {
        var msg = DnsMessage.TryParse(rawRequest);
        if (msg == null)
        {
            return DnsResponseBuilder.BuildRcodeResponseWithEde(rawRequest, 3, EdeCodeBlocked);
        }

        string customIp = _options.BlockResponse?.StaticIp ?? "0.0.0.0";
        if (!IPAddress.TryParse(customIp, out var ip))
        {
            ip = IPAddress.Any;
        }

        string qType = msg.QuestionType;

        if ((string.Equals(qType, "A", StringComparison.OrdinalIgnoreCase) || qType == "1") &&
            ip.AddressFamily == AddressFamily.InterNetwork)
        {
            return DnsResponseBuilder.BuildStaticIpResponse(rawRequest, ip);
        }

        if ((string.Equals(qType, "AAAA", StringComparison.OrdinalIgnoreCase) || qType == "28") &&
            ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return DnsResponseBuilder.BuildStaticIpResponse(rawRequest, ip);
        }

        return DnsResponseBuilder.BuildRcodeResponseWithEde(rawRequest, 3, EdeCodeBlocked);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Cache.Dispose();
    }
}
