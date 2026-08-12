using System;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Ntp;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Xunit;

namespace Astrolabed.Ntp.Tests;

public class UpstreamNtpTimeSourceTests
{
    private static UpstreamNtpTimeSource CreateInstance()
    {
        var options = new NtpServerOptions
        {
            Enabled = true,
            Upstream = new UpstreamNtpOptions
            {
                Enabled = false,
                Servers = new[] { "127.0.0.1" },
                PollIntervalSeconds = 16
            }
        };

        return new UpstreamNtpTimeSource(
            NullLogger<UpstreamNtpTimeSource>.Instance,
            Options.Create(options));
    }

    private static byte[] BuildFakeResponse(DateTime t2, DateTime t3, int stratum = 2)
    {
        var buffer = new byte[48];
        buffer[0] = 0b_00100100; // LI=0, VN=4, Mode=4 (server)
        buffer[1] = (byte)stratum;

        NtpTimestamp.WriteTimestamp(buffer.AsSpan(32, 8), t2);
        NtpTimestamp.WriteTimestamp(buffer.AsSpan(40, 8), t3);

        return buffer;
    }

    [Fact]
    public async Task Upstream_ParsesOffsetAndUpdatesStateCorrectly()
    {
        await using var upstream = CreateInstance();

        var t1 = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var t2 = t1.AddMilliseconds(10);
        var t3 = t1.AddMilliseconds(15);
        var t4 = t1.AddMilliseconds(25);

        var packet = BuildFakeResponse(t2, t3, stratum: 2);
        var serverIp = IPAddress.Parse("127.0.0.1");

        var parseMethod = typeof(UpstreamNtpTimeSource).GetMethod(
            "ParseResponse",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(parseMethod);

        parseMethod.Invoke(upstream, new object[] { packet, t1, t4, serverIp });

        var timeResult = await upstream.GetTimeAsync(CancellationToken.None);
        var expectedOffset = ((t2 - t1) + (t3 - t4)) / 2;

        Assert.Equal(expectedOffset, timeResult.Offset);
        Assert.Equal(3, timeResult.Stratum); // stratum = incomingStratum + 1
        Assert.True((timeResult.ReferenceUtc - t3).Duration() < TimeSpan.FromMicroseconds(10));
    }
}
