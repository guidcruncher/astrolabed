using System.Buffers.Binary;

using Astrolabed.Dns.Core;

using Xunit;

namespace Astrolabed.Dns.Tests
{
    public class DnsBlockedResponseTests
    {
        [Fact]
        public void BuildBlockedResponse_EchoesQuestionAndSetsNxDomain()
        {
            var req = new DnsMessage
            {
                Id = 0x1234
            };

            req.Questions.Add(new DnsQuestion { Name = "example.com", Type = DnsType.A, Class = 1 });

            var resp = DnsParser.BuildBlockedResponse(req);

            var parsed = DnsParser.Parse(resp);

            Assert.Equal(DnsResponseCode.NonExistentDomain, parsed.ResponseCode);
            Assert.Single(parsed.Questions);
            Assert.Equal("example.com", parsed.Questions[0].Name);
            Assert.Equal(DnsType.A, parsed.Questions[0].Type);
            Assert.Equal((ushort)1, parsed.Questions[0].Class);
        }
    }
}
