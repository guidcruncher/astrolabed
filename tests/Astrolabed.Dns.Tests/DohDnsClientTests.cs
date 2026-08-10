using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Dns.Core;

using Xunit;

namespace Astrolabed.Tests
{
    public class DohDnsClientTests
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

        [Fact]
        public async Task QueryAsync_PostsWireMessage_AndReturnsResponseBytes()
        {
            // Arrange: simple DNS query bytes (12 byte header + minimal question for example.com)
            var query = BuildSimpleQuery("example.com");

            var expectedResponse = new byte[] { 0, 1, 0x81, 0x80, 0, 1, 0, 1, 0, 0, 0, 0 }; // minimal response header

            var handler = new TestHandler(async req =>
            {
                // Assert request method and headers
                Assert.Equal(HttpMethod.Post, req.Method);
		Assert.NotNull(req);
                Assert.NotNull(req.Content);
		Assert.NotNull(req.Content.Headers.ContentType);
		Assert.NotNull(req.Content.Headers.ContentType.MediaType);
                Assert.Equal("application/dns-message", req.Content.Headers.ContentType.MediaType);

                var body = await req.Content.ReadAsByteArrayAsync();
                Assert.Equal(query, body);

                var resp = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(expectedResponse)
                };
                resp.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/dns-message");

                return resp;
            });

            var http = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://doh.test")
            };

            var client = new DohDnsClient(http, new Uri("https://doh.test/dns-query"));

            // Act
            var got = await client.QueryAsync(query, CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse, got);
        }

        private static byte[] BuildSimpleQuery(string name)
        {
            // Build a wire-format DNS query for name with one question of type A class IN
            var header = new byte[12];
            // ID
            header[0] = 0x12; header[1] = 0x34;
            // flags: RD=1
            header[2] = 0x01; header[3] = 0x00;
            // QDCOUNT = 1
            header[4] = 0x00; header[5] = 0x01;

            var nameParts = name.Split('.');
            var nameBuf = new List<byte>();
            foreach (var p in nameParts)
            {
                var bytes = Encoding.ASCII.GetBytes(p);
                nameBuf.Add((byte)bytes.Length);
                nameBuf.AddRange(bytes);
            }
            nameBuf.Add(0x00); // term

            // QTYPE = A
            nameBuf.Add(0x00); nameBuf.Add(0x01);
            // QCLASS = IN
            nameBuf.Add(0x00); nameBuf.Add(0x01);

            var buf = new byte[header.Length + nameBuf.Count];
            Buffer.BlockCopy(header, 0, buf, 0, header.Length);
            Buffer.BlockCopy(nameBuf.ToArray(), 0, buf, header.Length, nameBuf.Count);
            return buf;
        }
    }
}
