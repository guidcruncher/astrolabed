using System;
using System.Net;
using System.Net.Http;

namespace Astrolabed.Dns.Core;

public sealed class DefaultDnsClientFactory : IDnsClientFactory
{
    private static readonly IPEndPoint DefaultFallbackEndPoint = new(new IPAddress(new byte[] { 8, 8, 8, 8 }), 53);

    private readonly IHttpClientFactory _httpFactory;

    public DefaultDnsClientFactory(IHttpClientFactory httpFactory)
    {
        ArgumentNullException.ThrowIfNull(httpFactory);
        _httpFactory = httpFactory;
    }

    public IDnsClient Create(Dns.UpstreamResolverOptions resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);

        string address = resolver.Address?.Trim() ?? string.Empty;

        if (Uri.TryCreate(address, UriKind.Absolute, out var endpoint) &&
            (endpoint.Scheme == Uri.UriSchemeHttps || endpoint.Scheme == Uri.UriSchemeHttp))
        {
            string clientName = !string.IsNullOrWhiteSpace(resolver.Name)
                ? $"doh-{resolver.Name}"
                : $"doh-{address}";

            var http = _httpFactory.CreateClient(clientName);
            return new DohDnsClient(http, endpoint, preferPost: true);
        }

        int port = resolver.Port > 0 ? resolver.Port : 53;

        if (IPAddress.TryParse(address, out var ip))
        {
            return new UdpDnsClient(new IPEndPoint(ip, port));
        }

        if (!string.IsNullOrWhiteSpace(address))
        {
            try
            {
                var addrs = System.Net.Dns.GetHostAddresses(address);
                if (addrs.Length > 0)
                {
                    return new UdpDnsClient(new IPEndPoint(addrs[0], port));
                }
            }
            catch
            {
                // Fallback below
            }
        }

        return new UdpDnsClient(DefaultFallbackEndPoint);
    }
}
