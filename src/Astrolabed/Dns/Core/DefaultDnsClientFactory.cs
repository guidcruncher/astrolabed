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

        if (Uri.TryCreate(address, UriKind.Absolute, out var endpoint))
        {
            if (endpoint.Scheme == Uri.UriSchemeHttps || endpoint.Scheme == Uri.UriSchemeHttp)
            {
                string clientName = !string.IsNullOrWhiteSpace(resolver.Name)
                    ? $"doh-{resolver.Name}"
                    : $"doh-{address}";

                var http = _httpFactory.CreateClient(clientName);
                return new DohDnsClient(http, endpoint, preferPost: true);
            }

            if (endpoint.Scheme.Equals("tcp", StringComparison.OrdinalIgnoreCase) ||
                endpoint.Scheme.Equals("dns+tcp", StringComparison.OrdinalIgnoreCase))
            {
                string host = endpoint.Host;
                int tcpPort = endpoint.Port > 0 ? endpoint.Port : (resolver.Port > 0 ? resolver.Port : 53);

                if (IPAddress.TryParse(host, out var tcpIp))
                {
                    return new TcpDnsClient(new IPEndPoint(tcpIp, tcpPort));
                }

                if (!string.IsNullOrWhiteSpace(host))
                {
                    try
                    {
                        var addrs = System.Net.Dns.GetHostAddresses(host);
                        if (addrs.Length > 0)
                        {
                            return new TcpDnsClient(new IPEndPoint(addrs[0], tcpPort));
                        }
                    }
                    catch
                    {
                        // Fallback below
                    }
                }

                return new TcpDnsClient(DefaultFallbackEndPoint);
            }
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
