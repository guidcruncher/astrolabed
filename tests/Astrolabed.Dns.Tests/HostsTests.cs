using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

using Astrolabed.Dns.Core;
using Astrolabed.Dns.Filtering;
using Astrolabed.Dns.RuleEngine;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Xunit;

namespace Astrolabed.Dns.Tests;

public class HostsTests
{
    private RuleEngine.RuleEngine CreateEngine()
    {
        var opts = new DnsForwarderOptions
        {
            DefaultResolvers =
            {
                new UpstreamResolverOptions
                {
                    Name = "default",
                    Address = "1.1.1.1",
                    Port = 53,
                    Rule = null,
                    Block = false
                }
            },
            Resolvers = new List<UpstreamResolverOptions>()
        };

        var wrappedOpts = Options.Create(opts);
        var logger = NullLogger<RuleEngine.RuleEngine>.Instance;
        var clientFactory = new DefaultDnsClientFactory(new HttpClientFactoryStub());
        var cacheOptions = Options.Create(new CachingOptions { MaxEntries = 50 });
        var cacheLogger = NullLogger<DnsCache>.Instance;
        var cache = new DnsCache(cacheOptions, cacheLogger);
        return new RuleEngine.RuleEngine(wrappedOpts, logger, clientFactory, cache);
    }

    private static HostsFileSource CreateHostsSource(params string[] files)
    {
        return new HostsFileSource(files, NullLogger<HostsFileSource>.Instance);
    }

    [Fact]
    public async Task HostsFile_Should_Load_Entries()
    {
        var tmp = Path.GetTempFileName();
        await File.WriteAllLinesAsync(tmp, new[]
        {
            "127.0.0.1 localhost",
            "192.168.1.10 nas.local # inline comment",
            "# full line comment",
            "  "
        });

        var engine = CreateEngine();
        var source = CreateHostsSource(tmp);

        await engine.AddHostsAsync(source);

        var result1 = engine.Match("localhost", "-");
        var result2 = engine.Match("nas.local", "--");

        Assert.False(result1.Block);
        Assert.False(result2.Block);

        Assert.IsType<StaticDnsClient>(result1.Upstreams[0].Client);
        Assert.IsType<StaticDnsClient>(result2.Upstreams[0].Client);
    }

    [Fact]
    public async Task HostsEntry_Should_Override_DefaultResolver()
    {
        var tmp = Path.GetTempFileName();
        await File.WriteAllLinesAsync(tmp, new[]
        {
            "10.0.0.5 internal.local"
        });

        var engine = CreateEngine();
        var source = CreateHostsSource(tmp);

        await engine.AddHostsAsync(source);

        var result = engine.Match("internal.local", "---");

        Assert.False(result.Block);
        Assert.IsType<StaticDnsClient>(result.Upstreams[0].Client);
        Assert.Equal("hosts", result.Upstreams[0].Name);
    }

    [Fact]
    public async Task HostsWildcard_Suffix_Should_Match()
    {
        var tmp = Path.GetTempFileName();
        await File.WriteAllLinesAsync(tmp, new[]
        {
            "10.0.0.1 *.example.com"
        });

        var engine = CreateEngine();
        await engine.AddHostsAsync(CreateHostsSource(tmp));

        var result = engine.Match("foo.example.com", "suffix");

        Assert.False(result.Block);
        Assert.IsType<StaticDnsClient>(result.Upstreams[0].Client);
        Assert.Equal("hosts", result.Upstreams[0].Name);
    }

    [Fact]
    public async Task HostsWildcard_Prefix_Should_Match()
    {
        var tmp = Path.GetTempFileName();
        await File.WriteAllLinesAsync(tmp, new[]
        {
            "10.0.0.2 example.*"
        });

        var engine = CreateEngine();
        await engine.AddHostsAsync(CreateHostsSource(tmp));

        var result = engine.Match("example.domain", "prefix");

        Assert.False(result.Block);
        Assert.IsType<StaticDnsClient>(result.Upstreams[0].Client);
        Assert.Equal("hosts", result.Upstreams[0].Name);
    }

    [Fact]
    public async Task HostsWildcard_Substring_Should_Match()
    {
        var tmp = Path.GetTempFileName();
        await File.WriteAllLinesAsync(tmp, new[]
        {
            "10.0.0.3 *ads*"
        });

        var engine = CreateEngine();
        await engine.AddHostsAsync(CreateHostsSource(tmp));

        var result = engine.Match("superadsdomain.com", "substring");

        Assert.False(result.Block);
        Assert.IsType<StaticDnsClient>(result.Upstreams[0].Client);
        Assert.Equal("hosts", result.Upstreams[0].Name);
    }

    [Fact]
    public async Task HostsWildcard_LongestCoreWins()
    {
        var tmp = Path.GetTempFileName();
        await File.WriteAllLinesAsync(tmp, new[]
        {
            "10.0.0.10 *.example.com",
            "10.0.0.20 *ample.com",
            "10.0.0.30 *ple.com"
        });

        var engine = CreateEngine();
        await engine.AddHostsAsync(CreateHostsSource(tmp));

        var result = engine.Match("foo.example.com", "specificity");

        var client = Assert.IsType<StaticDnsClient>(result.Upstreams[0].Client);
        Assert.Equal("hosts", result.Upstreams[0].Name);

        var ipField = typeof(StaticDnsClient).GetField("_ip", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var ip = (IPAddress)ipField!.GetValue(client)!;

        Assert.Equal(IPAddress.Parse("10.0.0.10"), ip);
    }

    private class HttpClientFactoryStub : System.Net.Http.IHttpClientFactory
    {
        public System.Net.Http.HttpClient CreateClient(string name) => new System.Net.Http.HttpClient();
    }
}
