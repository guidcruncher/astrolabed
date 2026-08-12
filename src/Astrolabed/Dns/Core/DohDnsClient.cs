using System;
using System.Buffers.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Astrolabed.Dns.Core;

/// <summary>
/// DNS-over-HTTPS client implementing RFC 8484.
/// Sends/receives raw DNS wire-format bytes as application/dns-message.
/// Uses HttpClient provided (prefer IHttpClientFactory) to get HTTP/2/TLS benefits.
/// </summary>
public sealed class DohDnsClient : IDnsClient
{
    private const string MediaType = "application/dns-message";
    private static readonly MediaTypeHeaderValue DnsMediaTypeHeader = new(MediaType);
    private static readonly MediaTypeWithQualityHeaderValue DnsAcceptHeader = new(MediaType);

    private readonly HttpClient _http;
    private readonly Uri _endpoint;
    private readonly bool _preferPost;

    public DohDnsClient(HttpClient httpClient, Uri endpoint, bool preferPost = true)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(endpoint);

        _http = httpClient;
        _endpoint = endpoint;
        _preferPost = preferPost;
    }

    public async Task<byte[]> QueryAsync(byte[] request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var httpRequest = CreateRequestMessage(request);

        using var resp = await _http.SendAsync(httpRequest, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        var contentType = resp.Content.Headers.ContentType;
        if (contentType?.MediaType == null ||
            !contentType.MediaType.Equals(MediaType, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Upstream returned unexpected content-type: {contentType?.MediaType ?? "none"}");
        }

        var body = await resp.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);

        if (body.Length < 12)
        {
            throw new InvalidOperationException("Upstream DoH response too short");
        }

        return body;
    }

    private HttpRequestMessage CreateRequestMessage(byte[] request)
    {
        HttpRequestMessage req;

        if (_preferPost)
        {
            req = new HttpRequestMessage(HttpMethod.Post, _endpoint)
            {
                Content = new ReadOnlyMemoryContent(request)
                {
                    Headers = { ContentType = DnsMediaTypeHeader }
                }
            };
        }
        else
        {
            string b64 = Base64Url.EncodeToString(request);
            string baseUriStr = _endpoint.AbsoluteUri;
            string requestUri = baseUriStr.Contains('?')
                ? $"{baseUriStr}&dns={b64}"
                : $"{baseUriStr}?dns={b64}";

            req = new HttpRequestMessage(HttpMethod.Get, requestUri);
        }

        req.Headers.Accept.Add(DnsAcceptHeader);
        return req;
    }
}
