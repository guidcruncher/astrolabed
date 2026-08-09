
using System.Buffers.Binary;
using System.Net.Sockets;

using Astrolabed;

using Microsoft.Extensions.Logging;

namespace Astrolabed.Ntp;

public interface INtpRequestHandler
{
    Task<NtpResponse> HandleAsync(UdpReceiveResult result, UdpClient udp, CancellationToken ct);
}

