using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace Astrolabed.Ntp;

public sealed class NtpRequestHandler : INtpRequestHandler
{
    private readonly ILogger<NtpRequestHandler> _logger;
    private readonly INtpTimeSource _timeSource;

    public NtpRequestHandler(
        ILogger<NtpRequestHandler> logger,
        INtpTimeSource timeSource)
    {
        _logger = logger;
        _timeSource = timeSource;
    }

    public async Task<NtpResponse> HandleAsync(
        UdpReceiveResult result,
        UdpClient udp,
        CancellationToken ct)
    {
        var receiveUtc = DateTime.UtcNow;

        try
        {
            var request = NtpPacket.Parse(result.Buffer);

            var upstream = await _timeSource.GetTimeAsync(ct);
            var transmitUtc = DateTime.UtcNow;

            var correctedReceiveUtc = receiveUtc + upstream.Offset;
            var correctedTransmitUtc = transmitUtc + upstream.Offset;

            // Retrieve ReferenceId from the time source if available, or default to LOCL (0x4C4F434C)
            uint referenceId = (_timeSource as SlewedUpstreamNtpTimeSource)?.GetCurrentReferenceId()
                ?? 0x4C4F434C;

            var responsePacket = NtpPacket.BuildResponse(
                request,
                correctedReceiveUtc,
                correctedTransmitUtc,
                upstream.Stratum,
                referenceId);

            var bytes = responsePacket.ToBytes();

            return new NtpResponse(
                Success: true,
                Offset: upstream.Offset,
                Bytes: bytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to process NTP request from {Remote}",
                result.RemoteEndPoint);

            return new NtpResponse(
                Success: false,
                Offset: TimeSpan.Zero,
                Bytes: Array.Empty<byte>());
        }
    }

}
