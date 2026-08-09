using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Astrolabed.Dns.Core
{
    /// <summary>
    /// DNS-over-HTTPS client implementing RFC 8484.
    /// Sends/receives raw DNS wire-format bytes as application/dns-message.
    /// Uses HttpClient provided (prefer IHttpClientFactory) to get HTTP/2/TLS benefits.
    /// </summary>
    public sealed class DohDnsClient : IDnsClient
    {
        private readonly HttpClient _http;
        private readonly Uri _endpoint;
        private readonly bool _preferPost;
        private const string MediaType = "application/dns-message";

        public DohDnsClient(HttpClient httpClient, Uri endpoint, bool preferPost = true)
        {
            _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _preferPost = preferPost;

            // Ensure Accept header for DoH
            if (!_http.DefaultRequestHeaders.Accept.Contains(new MediaTypeWithQualityHeaderValue(MediaType)))
                _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaType));
        }

        public async Task<byte[]> QueryAsync(byte[] request, CancellationToken ct)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            HttpResponseMessage resp;
            if (_preferPost)
            {
                using var content = new ByteArrayContent(request);
                content.Headers.ContentType = new MediaTypeHeaderValue(MediaType);
                resp = await _http.PostAsync(_endpoint, content, ct).ConfigureAwait(false);
            }
            else
            {
                // GET: base64url encode without padding
                var b64 = Convert.ToBase64String(request)
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .TrimEnd('=');
                var builder = new UriBuilder(_endpoint);
                var q = builder.Query;
                if (!string.IsNullOrEmpty(q) && q.StartsWith("?")) q = q.Substring(1);
                builder.Query = string.IsNullOrEmpty(q) ? $"dns={b64}" : q + "&dns=" + b64;
                resp = await _http.GetAsync(builder.Uri, ct).ConfigureAwait(false);
            }

            resp.EnsureSuccessStatusCode();

            // Validate content-type (best-effort)
            if (resp.Content.Headers.ContentType == null ||
                !resp.Content.Headers.ContentType.MediaType.Equals(MediaType, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Upstream returned unexpected content-type: {resp.Content.Headers.ContentType}");
            }

            var body = await resp.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);

            if (body == null || body.Length < 12)
                throw new InvalidOperationException("Upstream DoH response too short");

            return body;
        }
    }
}
