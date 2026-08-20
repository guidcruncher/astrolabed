// File: src/Astrolabed.Dns/Upstream/DoHUpstreamDnsClient.cs
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Dns.Models;
using Astrolabed.Dns.Serialization;

namespace Astrolabed.Dns.Upstream;

public class DoHUpstreamDnsClient : IDnsUpstreamClient
{
    private readonly HttpClient _httpClient;
    private const string DohContentType = "application/dns-message";

    public DoHUpstreamDnsClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<DnsWireMessage?> QueryAsync(IPAddress targetServer, byte[] rawRequest, CancellationToken ct)
    {
        try
        {
            // DoH Target via HTTPS POST endpoint (RFC 8484)
            var uri = new Uri($"https://{targetServer}/dns-query");
            using var request = new HttpRequestMessage(HttpMethod.Post, uri);

            request.Content = new ByteArrayContent(rawRequest);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(DohContentType);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(DohContentType));

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return null;

            var responseBytes = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);

            if (DnsWireParser.TryParse(responseBytes, out var message) && message != null)
            {
                return message;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
