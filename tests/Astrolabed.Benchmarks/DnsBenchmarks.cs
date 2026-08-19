using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Astrolabed.Dns.Core;
using Astrolabed.Dns.RuleEngine;
using Astrolabed.Events;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Benchmarks;

public class DnsBenchmarks
{
    private class HttpClientFactoryStub : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new HttpClient();
    }

    private byte[] _query = Array.Empty<byte>();
    private Astrolabed.Dns.RuleEngine.RuleEngine _engine = default!;
    private DnsCache _cache = default!;
    private DnsRequestContext _context = default!;
    private byte[] _sampleResponse = Array.Empty<byte>();

    [GlobalSetup]
    public void Setup()
    {
        _query = new byte[]
        {
            0x12, 0x34, 0x01, 0x00,
            0x00, 0x01, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x07, (byte)'e',(byte)'x',(byte)'a',(byte)'m',(byte)'p',(byte)'l',(byte)'e',
            0x03, (byte)'c',(byte)'o',(byte)'m',
            0x00,
            0x00, 0x01,
            0x00, 0x01
        };

        _sampleResponse = new byte[]
        {
            0x12, 0x34, 0x81, 0x80,
            0x00, 0x01, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x00,
            0x07, (byte)'e',(byte)'x',(byte)'a',(byte)'m',(byte)'p',(byte)'l',(byte)'e',
            0x03, (byte)'c',(byte)'o',(byte)'m',
            0x00,
            0x00, 0x01, 0x00, 0x01,
            0xC0, 0x0C,
            0x00, 0x01, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x3C,
            0x00, 0x04,
            0x5D, 0xB8, 0xD8, 0x22
        };

        var options = new DnsForwarderOptions
        {
            Resolvers =
            {
                new UpstreamResolverOptions
                {
                    Name = "Internal",
                    Rule = "^(.+\\.corp\\.local)$",
                    Address = "10.0.0.10",
                    Port = 53,
                    Block = false
                },
                new UpstreamResolverOptions
                {
                    Name = "BlockAds",
                    Rule = "^(ads|tracking)\\.",
                    Block = true
                }
            },
            DefaultResolvers =
            {
                new UpstreamResolverOptions
                {
                    Name = "Cloudflare",
                    Address = "1.1.1.1",
                    Port = 53,
                    Block = false
                },
                new UpstreamResolverOptions
                {
                    Name = "Google",
                    Address = "8.8.8.8",
                    Port = 53,
                    Block = false
                }
            },
            Caching = new CachingOptions
            {
                Enabled = true,
                MaxEntries = 10000
            }
        };

        var wrappedOptions = Options.Create(options);
        var logger = NullLogger<Astrolabed.Dns.RuleEngine.RuleEngine>.Instance;
        var clientFactory = new DefaultDnsClientFactory(new HttpClientFactoryStub());

        var cacheOptions = Options.Create(new CachingOptions { MaxEntries = 50 });
        var cacheLogger = NullLogger<DnsCache>.Instance;
        _cache = new DnsCache(cacheOptions, cacheLogger);
        _engine = new Astrolabed.Dns.RuleEngine.RuleEngine(wrappedOptions, logger, clientFactory, _cache, new NullDnsMetrics());

        _context = new DnsRequestContext(_query, "benchmark-id", "127.0.0.1", "localhost");

        // Warm cache
        _cache.Store(_context, _sampleResponse, TimeSpan.FromMinutes(5));
    }

    [Benchmark]
    public void Parse_Dns_Query()
    {
        var msg = DnsParser.Parse(_query);
        _ = msg.Questions.Count;
    }

    [Benchmark]
    public void RuleEngine_Match_Default()
    {
        var result = _engine.Match("example.com", "-");
        _ = result.Upstreams;
    }

    [Benchmark]
    public void RuleEngine_Match_Block()
    {
        var result = _engine.Match("ads.example.com", "--");
        _ = result.Block;
    }

    [Benchmark]
    public void Cache_Hit()
    {
        if (_cache.TryGet(_context, out var response) && response != null)
        {
            _ = response.Length;
        }
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<DnsBenchmarks>();
    }
}
