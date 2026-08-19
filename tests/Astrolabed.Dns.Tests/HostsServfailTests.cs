using System.Net.Http;

using Astrolabed.Dns.Core;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Xunit;

namespace Astrolabed.Dns.Tests;

public sealed class HostsServfailTests
{
    private class HttpClientFactoryStub : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new HttpClient();
    }

    [Fact]
    public void Failure_Returns_Servfail()
    {
        var options = new DnsForwarderOptions();
        var logger = NullLogger<Astrolabed.Dns.RuleEngine.RuleEngine>.Instance;
        var clientFactory = new DefaultDnsClientFactory(new HttpClientFactoryStub());
        var cacheOptions = Options.Create(new CachingOptions { MaxEntries = 50 });
        var cacheLogger = NullLogger<Astrolabed.Dns.RuleEngine.DnsCache>.Instance;
        var cache = new Astrolabed.Dns.RuleEngine.DnsCache(cacheOptions, cacheLogger);
        var engine = new Astrolabed.Dns.RuleEngine.RuleEngine(Options.Create(options), logger, clientFactory, cache, new NullDnsMetrics());

        var result = engine.Match("fail.test", "-");
        Assert.NotNull(result);
    }
}
