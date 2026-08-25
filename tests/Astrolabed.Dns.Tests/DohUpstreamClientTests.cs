using System.Net;
using System.Net.Http.Headers;

using Astrolabed.Dns.Models;
using Astrolabed.Dns.Upstream;

using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace Astrolabed.Dns.Tests;

public class DoHUpstreamDnsClientTests
{
    [Fact]
    public void Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new DoHUpstreamDnsClient(null!, NullLogger<DoHUpstreamDnsClient>.Instance));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        using var httpClient = new HttpClient();
        Assert.Throws<ArgumentNullException>(() =>
            new DoHUpstreamDnsClient(httpClient, null!));
    }

    [Fact]
    public async Task QueryAsync_NullTargetServer_ThrowsArgumentNullException()
    {
        using var httpClient = new HttpClient();
        var client = new DoHUpstreamDnsClient(httpClient, NullLogger<DoHUpstreamDnsClient>.Instance);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            client.QueryAsync(null!, new byte[] { 0x01 }));
    }

    [Fact]
    public async Task QueryAsync_HttpErrorStatusCode_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        using var httpClient = new HttpClient(handler);
        var client = new DoHUpstreamDnsClient(httpClient, NullLogger<DoHUpstreamDnsClient>.Instance);

        DnsWireMessage? result = await client.QueryAsync(IPAddress.Loopback, new byte[] { 0x00, 0x01 });

        Assert.Null(result);
    }

    [Fact]
    public async Task QueryAsync_ValidWireResponse_ReturnsParsedMessage()
    {
        // Minimal valid 12-byte DNS Header wire payload
        byte[] validDnsResponse = [
            0x12, 0x34, // Transaction ID
            0x80, 0x00, // Flags: Response
            0x00, 0x00, // QDCOUNT
            0x00, 0x00, // ANCOUNT
            0x00, 0x00, // NSCOUNT
            0x00, 0x00  // ARCOUNT
        ];

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(validDnsResponse)
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/dns-message");

        var handler = new MockHttpMessageHandler(response);
        using var httpClient = new HttpClient(handler);
        var client = new DoHUpstreamDnsClient(httpClient, NullLogger<DoHUpstreamDnsClient>.Instance);

        DnsWireMessage? result = await client.QueryAsync(IPAddress.Loopback, validDnsResponse);

        Assert.NotNull(result);
        Assert.Equal(0x1234, result.TransactionId);
        Assert.True(result.IsResponse);
    }

    private sealed class MockHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(response);
        }
    }
}
