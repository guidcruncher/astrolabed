using System.Buffers;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;

using Astrolabed.Dns.Models;
using Astrolabed.Dns.Serialization;

using Microsoft.Extensions.Logging;

namespace Astrolabed.Dns.Upstream;

/// <summary>
/// Provides RFC 8484 compliant DNS over HTTPS (DoH) upstream resolution using high-performance HTTP client operations.
/// </summary>
/// <param name="httpClient">The configured HTTP client instance.</param>
/// <param name="logger">Structured logger instance.</param>
public sealed partial class DoHUpstreamDnsClient(
    HttpClient httpClient,
    ILogger<DoHUpstreamDnsClient> logger) : IDnsUpstreamClient
{
    private const string DohContentType = "application/dns-message";

    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly ILogger<DoHUpstreamDnsClient> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<DnsWireMessage?> QueryAsync(IPAddress targetServer, byte[] rawRequest, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(targetServer);
        ArgumentNullException.ThrowIfNull(rawRequest);

        try
        {
            string hostStr = targetServer.AddressFamily == AddressFamily.InterNetworkV6
                ? $"[{targetServer}]"
                : targetServer.ToString();

            var uri = new Uri($"https://{hostStr}/dns-query");

            using var request = new HttpRequestMessage(HttpMethod.Post, uri);
            using var content = new ReadOnlyMemoryContent(rawRequest);
            content.Headers.ContentType = new MediaTypeHeaderValue(DohContentType);

            request.Content = content;
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(DohContentType));

            using HttpResponseMessage response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                LogDohRequestFailed(_logger, targetServer, (int)response.StatusCode);
                return null;
            }

            await using Stream responseStream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);

            try
            {
                int bytesRead = await responseStream.ReadAsync(buffer, ct).ConfigureAwait(false);

                if (DnsWireParser.TryParse(buffer.AsSpan(0, bytesRead), out DnsWireMessage? message) && message is not null)
                {
                    return message;
                }

                LogDohWireParseFailed(_logger, targetServer);
                return null;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogDohQueryException(_logger, targetServer, ex);
            return null;
        }
    }

    [LoggerMessage(
        EventId = 101,
        Level = LogLevel.Warning,
        Message = "DoH DNS query to target server {TargetServer} failed with HTTP status code {StatusCode}")]
    private static partial void LogDohRequestFailed(ILogger logger, IPAddress targetServer, int statusCode);

    [LoggerMessage(
        EventId = 102,
        Level = LogLevel.Warning,
        Message = "Failed to parse DNS wire message payload from DoH server {TargetServer}")]
    private static partial void LogDohWireParseFailed(ILogger logger, IPAddress targetServer);

    [LoggerMessage(
        EventId = 103,
        Level = LogLevel.Error,
        Message = "Exception encountered during DoH DNS query to target server {TargetServer}")]
    private static partial void LogDohQueryException(ILogger logger, IPAddress targetServer, Exception exception);
}
