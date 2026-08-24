using System.Net;
using System.Net.Sockets;

using Astrolabed.Dhcp.Options;
using Astrolabed.Dhcp.Protocol;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dhcp.Services;

/// <summary>
/// Core DHCP hosted background engine responsible for receiving incoming UDP packets,
/// executing scoped processing pipelines, and broadcasting server responses.
/// </summary>
/// <param name="scopeFactory">Factory for creating scoped service resolution contexts per message.</param>
/// <param name="options">Monitored options instance containing runtime DHCP server configuration.</param>
/// <param name="logger">Structured logging engine instance.</param>
public sealed partial class DhcpEngine(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<DhcpServerOptions> options,
    ILogger<DhcpEngine> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    private readonly IOptionsMonitor<DhcpServerOptions> _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger<DhcpEngine> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        DhcpServerOptions config = _options.CurrentValue;

        if (!config.ListenAddress.Enabled)
        {
            _logger.LogWarning("DHCP Server is not enabled.");
            return;
        }

        if (!IPAddress.TryParse(config.ListenAddress.Address, out IPAddress? bindIp))
        {
            bindIp = IPAddress.Any;
        }

        int bindPort = config.TestMode ? config.TestPort : config.ListenAddress.Port;

        using var udpClient = new UdpClient();
        if (!config.TestMode)
        {
            udpClient.EnableBroadcast = true;
        }

        udpClient.Client.Bind(new IPEndPoint(bindIp, bindPort));
        LogDhcpEngineStarted(_logger, bindIp, bindPort, config.TestMode);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                UdpReceiveResult result = await udpClient.ReceiveAsync(stoppingToken).ConfigureAwait(false);
                DhcpMessage requestMessage = DhcpDecoder.Decode(result.Buffer);

                await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
                var handler = scope.ServiceProvider.GetRequiredService<IDhcpHandler>();

                DhcpMessage? responseMessage = await handler.ProcessMessageAsync(requestMessage, stoppingToken).ConfigureAwait(false);

                if (responseMessage is not null)
                {
                    byte[] responseBytes = DhcpEncoder.Encode(responseMessage);
                    IPEndPoint destination = config.TestMode
                        ? result.RemoteEndPoint
                        : new IPEndPoint(IPAddress.Broadcast, result.RemoteEndPoint.Port == 0 ? 68 : result.RemoteEndPoint.Port);

                    await udpClient.SendAsync(responseBytes, responseBytes.Length, destination).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogPacketProcessingError(_logger, ex);
            }
        }
    }

    [LoggerMessage(
        EventId = 101,
        Level = LogLevel.Information,
        Message = "DHCP Listener started on {Address}:{Port} (TestMode: {TestMode})")]
    private static partial void LogDhcpEngineStarted(ILogger logger, IPAddress address, int port, bool testMode);

    [LoggerMessage(
        EventId = 501,
        Level = LogLevel.Error,
        Message = "Error occurred processing DHCP packet.")]
    private static partial void LogPacketProcessingError(ILogger logger, Exception exception);
}
