using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Astrolabed.Ntp;

public interface INtpRequestHandler
{
    Task<NtpResponse> HandleAsync(UdpReceiveResult result, UdpClient udp, CancellationToken ct);
}

