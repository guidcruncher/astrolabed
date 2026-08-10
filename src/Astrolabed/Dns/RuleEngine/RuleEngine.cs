using System.Net.Http;

using Astrolabed.Dns.Core;
using Astrolabed.Dns.Filtering;
using Astrolabed.Utils;

using Microsoft.Extensions.Logging;

namespace Astrolabed.Dns.RuleEngine;

public sealed class RuleEngine
{
    private readonly ILogger<RuleEngine> _logger;
    private readonly DnsForwarderOptions _options;

    private readonly RuleCompiler _compiler;
    private readonly RuleMatcher _matcher;
    private readonly ResolverChainBuilder _chainBuilder;
    private readonly QueryExecutor _executor;
    private readonly BlockResponseBuilder _blockBuilder;

    private sealed class SimpleHttpClientFactory : IHttpClientFactory
    {
        private static readonly HttpClient SharedClient = new();
        public HttpClient CreateClient(string name) => SharedClient;
    }

    public DnsCache Cache { get; } = new();

    public RuleEngine(DnsForwarderOptions options, ILogger<RuleEngine> logger)
        : this(options, logger, new DefaultDnsClientFactory(new SimpleHttpClientFactory()))
    {
    }

    public RuleEngine(DnsForwarderOptions options, ILogger<RuleEngine> logger, IDnsClientFactory clientFactory)
    {
        _options = options;
        _logger = logger;

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

    public async Task<byte[]> QueryAsync(string domain, byte[] request, string? requestId, RuleResult match, CancellationToken ct)
    {
        bool isDebug = _logger.IsEnabled(LogLevel.Debug);

        if (Cache.TryGet(domain, out var cached) && cached != null)
        {
            if (isDebug)
            {
                _logger.LogDebug("Request {RequestId}: Cache HIT for {Domain}", requestId, domain);
            }

            // Copy cached buffer before mutating transaction ID bytes to prevent thread race/corruption
            var responseCopy = (byte[])cached.Clone();
            responseCopy[0] = request[0];
            responseCopy[1] = request[1];
            return responseCopy;
        }

        if (isDebug)
        {
            _logger.LogDebug("Request {RequestId}: Cache MISS for {Domain}", requestId, domain);
        }

        if (match.Block)
        {
            if (isDebug) _logger.LogDebug("Request {RequestId}: Blocked {Domain} using mode {Mode}",
                   requestId, domain, _options.BlockResponse.Mode);

            return _blockBuilder.BuildBlockResponse(request);
        }

        var upstreams = _chainBuilder.BuildChain(match, domain, requestId);

        var response = await _executor.ExecuteAsync(upstreams, domain, request, requestId, ct).ConfigureAwait(false);

        if (response == null)
        {
            _logger.LogError("Request {RequestId}: All upstreams failed for {Domain}, returning SERVFAIL",
                requestId, domain);

            return BlockResponseBuilder.BuildServfail(request);
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
}
