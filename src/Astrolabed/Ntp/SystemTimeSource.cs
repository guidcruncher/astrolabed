using System;
using System.Threading;
using System.Threading.Tasks;

namespace Astrolabed.Ntp;

public sealed class SystemTimeSource : INtpTimeSource
{
    public Task<NtpTimeResult> GetTimeAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var now = DateTime.UtcNow;

        return Task.FromResult(new NtpTimeResult(
            UtcNow: now,
            Offset: TimeSpan.Zero,
            Stratum: 1, // Stratum 1 = Primary reference (local system clock)
            ReferenceUtc: now));
    }
}
