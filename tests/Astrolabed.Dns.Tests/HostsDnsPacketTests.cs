using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

using Astrolabed.Dns.Core;
using Astrolabed.Dns.Filtering;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Xunit;

namespace Astrolabed.Dns.Tests;

public sealed class HostsDnsPacketTests
{
    private class HttpClientFactoryStub : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new HttpClient();
    }

    [Fact]
    public async Task Hosts_Match_Returns_Packet()
    {
        var options = new DnsForwarderOptions();
        var logger = NullLogger<Astrolabed.Dns.RuleEngine.RuleEngine>.Instance;
        var clientFactory = new DefaultDnsClientFactory(new HttpClientFactoryStub());
        var cache = new Astrolabed.Dns.RuleEngine.DnsCache(50);
        var engine = new Astrolabed.Dns.RuleEngine.RuleEngine(Options.Create(options), logger, clientFactory, cache);

        var tmp = Path.GetTempFileName();
        await File.WriteAllLinesAsync(tmp, new[] { "127.0.0.1 host.test" });

        var source = new HostsFileSource(new[] { tmp }, NullLogger<HostsFileSource>.Instance);
        await engine.AddHostsAsync(source);

        var result = engine.Match("host.test", "-");
        Assert.False(result.Block);
    }
}
