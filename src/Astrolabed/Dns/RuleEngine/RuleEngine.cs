using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Dns.Core;
using Astrolabed.Dns.Filtering;

using Microsoft.Extensions.Logging;

namespace Astrolabed.Dns.RuleEngine;

public sealed class RuleEngine : IDisposable
{
    private readonly ILogger<RuleEngine> _logger;
    private readonly DnsForwarderOptions _options;

    private readonly RuleCompiler _compiler;
    private readonly RuleMatcher _matcher;
    private readonly ResolverChainBuilder _chainBuilder;
    private readonly QueryExecutor _executor;
    private readonly BlockResponseBuilder _blockBuilder;
    private bool _disposed;

    private sealed class SimpleHttpClientFactory : IHttpClientFactory
    {
        private static readonly HttpClient SharedClient = new();
        public HttpClient CreateClient(string name) => SharedClient;
    }

    public DnsCache Cache { get; }

    public RuleEngine(DnsForwarderOptions options, ILogger<RuleEngine> logger)
        : this(options, logger, new DefaultDnsClientFactory(new SimpleHttpClientFactory()))
    {
    }

    public RuleEngine(DnsForwarderOptions options, ILogger<RuleEngine> logger, IDnsClientFactory clientFactory)
    {
        _options = options;
        _logger = logger;

        int maxEntries = options.Caching?.MaxEntries > 0 ? options.Caching.MaxEntries : 10000;
        int cleanupIntervalMinutes = options.Caching?.CleanupIntervalMinutes > 0 ? options.Caching.CleanupIntervalMinutes : 1;

        Cache = new DnsCache(maxEntries, TimeSpan.FromMinutes(cleanupIntervalMinutes));

        _compiler = new RuleCompiler(options, logger, clientFactory);
        _matcher = new RuleMatcher(_compiler, logger);
        _chainBuilder = new ResolverChainBuilder(options, _compiler.DefaultClient, _compiler.FallbackResolvers, clientFactory, logger);
        _blockBuilder = new BlockResponseBuilder(options);
        _executor = new QueryExecutor(Cache, logger);

        if (options.Resolvers != null)
        {
            foreach (var r in options.Resolvers)
            {
                _compiler.AddResolver(r);
            }
        }

        _compiler.BuildAutomata();
    }

    public async Task AddHostsAsync(HostsFileSource src)
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
                    context.RequestId, context.Domain, _options.BlockResponse.Mode);
            }

            return _blockBuilder.BuildBlockResponse(context.RawRequest);
        }

        var upstreams = _chainBuilder.BuildChain(match, context.Domain, context.RequestId);

        var response = await _executor.ExecuteAsync(upstreams, context, ct).ConfigureAwait(false);

        if (response == null)
        {
            _logger.LogError("Request {RequestId}: All upstreams failed for {Domain}, returning SERVFAIL",
                context.RequestId, context.Domain);

            return BlockResponseBuilder.BuildServfail(context.RawRequest);
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
