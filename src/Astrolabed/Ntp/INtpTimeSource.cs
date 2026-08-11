using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Astrolabed.Ntp;

public interface INtpTimeSource
{
    /// <summary>
    /// Returns the current disciplined time and offset.
    /// </summary>
    Task<NtpTimeResult> GetTimeAsync(CancellationToken ct);
}

