using System;
using System.Net;
using System.Net.Http;

namespace Astrolabed.Dns.Core
{
    public sealed class DefaultDnsClientFactory : IDnsClientFactory
    {
        private readonly IHttpClientFactory _httpFactory;

        public DefaultDnsClientFactory(IHttpClientFactory httpFactory)
        {
            _httpFactory = httpFactory ?? throw new ArgumentNullException(nameof(httpFactory));
        }

        public IDnsClient Create(Dns.UpstreamResolverOptions resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));

            if (!string.IsNullOrWhiteSpace(resolver.Address) &&
                (resolver.Address.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                 resolver.Address.StartsWith("http://", StringComparison.OrdinalIgnoreCase)))
            {
                var clientName = !string.IsNullOrWhiteSpace(resolver.Name)
                    ? $"doh-{resolver.Name}"
                    : $"doh-{resolver.Address}";

                var http = _httpFactory.CreateClient(clientName);
                http.Timeout = TimeSpan.FromSeconds(10);

                var endpoint = new Uri(resolver.Address, UriKind.Absolute);
                return new DohDnsClient(http, endpoint, preferPost: true);
            }
            else
            {
                // Plain IP or hostname -> UDP client
                if (IPAddress.TryParse(resolver.Address, out var ip))
                {
                    return new UdpDnsClient(new IPEndPoint(ip, resolver.Port));
                }

                try
                {
                    var addrs = System.Net.Dns.GetHostAddresses(resolver.Address);
                    if (addrs != null && addrs.Length > 0)
                        return new UdpDnsClient(new IPEndPoint(addrs[0], resolver.Port));
                }
                catch
                {
                    // ignore, fallback below
                }

                return new UdpDnsClient(new IPEndPoint(IPAddress.Parse("8.8.8.8"), 53));
            }
        }
    }
}
