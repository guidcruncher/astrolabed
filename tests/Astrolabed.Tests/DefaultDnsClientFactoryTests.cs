using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Astrolabed.Tests
{
    public class DefaultDnsClientFactoryTests
    {
        private class TestHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _responder;

            public TestHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder)
            {
                _responder = responder;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return _responder(request);
            }
        }

        private class SimpleHttpFactory : IHttpClientFactory
        {
            private readonly HttpClient _client;
            public SimpleHttpFactory(HttpClient client) => _client = client;
            public HttpClient CreateClient(string name) => _client;
        }

        [Fact]
        public void Create_ReturnsDohClient_ForHttpsResolver()
        {
            var handler = new TestHandler(req => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(new byte[] { 0 })
            }));

            var http = new HttpClient(handler) { BaseAddress = new Uri("https://doh.test") };
            var factory = new SimpleHttpFactory(http);

            var clientFactory = new Astrolabed.Dns.Core.DefaultDnsClientFactory(factory);

            var resolver = new Dns.UpstreamResolverOptions { Name = "test", Address = "https://doh.test/dns-query", Port = 443 };
            var client = clientFactory.Create(resolver);

            Assert.NotNull(client);
            Assert.IsType<Astrolabed.Dns.Core.DohDnsClient>(client);
        }

        [Fact]
        public void Create_ReturnsUdpClient_ForIpResolver()
        {
            var http = new HttpClient();
            var factory = new SimpleHttpFactory(http);
            var clientFactory = new Astrolabed.Dns.Core.DefaultDnsClientFactory(factory);

            var resolver = new Dns.UpstreamResolverOptions { Name = "ip", Address = "8.8.4.4", Port = 53 };
            var client = clientFactory.Create(resolver);

            Assert.NotNull(client);
            Assert.IsType<Astrolabed.Dns.Core.UdpDnsClient>(client);
        }
    }
}
