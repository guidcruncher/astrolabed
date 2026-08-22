using System.Net;
using System.Net.Sockets;

using Astrolabed.Ntp.Options;
using Astrolabed.Ntp.Protocol;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Ntp.Services;

public class NtpServerEngine : BackgroundService
{
    private readonly INtpServerHandler _handler;
    private readonly ITimeResolver _timeResolver;
    private readonly IOptionsMonitor<NtpServerOptions> _optionsMonitor;
    private readonly ILogger<NtpServerEngine> _logger;

    public NtpServerEngine(
        INtpServerHandler handler,
        ITimeResolver timeResolver,
        IOptionsMonitor<NtpServerOptions> optionsMonitor,
        ILogger<NtpServerEngine> logger)
    {
        _handler = handler;
        _timeResolver = timeResolver;
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        NtpServerOptions options = _optionsMonitor.CurrentValue;
        IPAddress ipAddress = IPAddress.Parse(options.ListenAddress.Address);
        IPEndPoint localEndPoint = new(ipAddress, options.ListenAddress.Port);

        using UdpClient udpClient = new(localEndPoint);
        _logger.LogInformation("NTP Server listening on {Endpoint}", localEndPoint);

        byte[] sendBuffer = new byte[512];

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                UdpReceiveResult receiveResult = await udpClient.ReceiveAsync(stoppingToken);
                DateTimeOffset receiveTime = await _timeResolver.GetCurrentTimeAsync(stoppingToken);

                if (receiveResult.Buffer.Length < NtpPacketSerializer.HeaderSize)
                {
                    _logger.LogWarning("Received invalid UDP datagram from {RemoteEndPoint}: packet too small ({Length} bytes)",
                        receiveResult.RemoteEndPoint, receiveResult.Buffer.Length);
                    continue;
                }

                NtpPacket requestPacket = NtpPacketSerializer.Deserialize(receiveResult.Buffer);

                if (requestPacket.Mode != NtpMode.Client)
                {
                    _logger.LogWarning("Received non-client NTP mode ({Mode}) from {RemoteEndPoint}",
                        requestPacket.Mode, receiveResult.RemoteEndPoint);
                    continue;
                }

                DateTimeOffset transmitTime = await _timeResolver.GetCurrentTimeAsync(stoppingToken);
                NtpPacket responsePacket = _handler.CreateResponse(requestPacket, receiveTime, transmitTime);

                int bytesWritten = NtpPacketSerializer.Serialize(responsePacket, sendBuffer);

                await udpClient.SendAsync(sendBuffer.AsMemory(0, bytesWritten), receiveResult.RemoteEndPoint, stoppingToken);

                _logger.LogTrace("Responded to NTP client {RemoteEndPoint}", receiveResult.RemoteEndPoint);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing NTP request");
            }
        }

        _logger.LogInformation("NTP Server service stopped.");
    }
}
