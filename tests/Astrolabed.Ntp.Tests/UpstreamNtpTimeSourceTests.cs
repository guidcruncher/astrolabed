using System;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Ntp;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Xunit;

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
                Servers = new[] { "0.pool.ntp.org" },
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
        buffer[0] = 0b_00100100;
        buffer[1] = (byte)stratum;

        NtpTimestamp.WriteTimestamp(buffer, 32, t2);
        NtpTimestamp.WriteTimestamp(buffer, 40, t3);

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

        var packet = BuildFakeResponse(t2, t3, stratum: 3);

        var parseMethod = typeof(UpstreamNtpTimeSource)
            .GetMethod("ParseResponse", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        parseMethod?.Invoke(upstream, new object[] { packet, t1, t4 });

        var timeResult = await upstream.GetTimeAsync(CancellationToken.None);
        var expectedOffset = ((t2 - t1) + (t3 - t4)) / 2;

        Assert.Equal(expectedOffset, timeResult.Offset);
        Assert.Equal(3, timeResult.Stratum);
        Assert.True((timeResult.ReferenceUtc - t3).Duration() < TimeSpan.FromMicroseconds(10));
    }
}
