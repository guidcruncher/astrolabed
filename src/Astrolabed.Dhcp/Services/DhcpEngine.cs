using System.Net;
using System.Net.Sockets;

using Astrolabed.Dhcp.Options;
using Astrolabed.Dhcp.Protocol;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dhcp.Services;

public class DhcpEngine : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<DhcpServerOptions> _options;
    private readonly ILogger<DhcpEngine> _logger;
    private readonly DhcpDecoder _decoder = new();
    private readonly DhcpEncoder _encoder = new();

    public DhcpEngine(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<DhcpServerOptions> options,
        ILogger<DhcpEngine> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = _options.CurrentValue;
        int bindPort = config.TestMode ? config.TestPort : config.Port;
        var bindIp = IPAddress.Parse(config.ListenAddress);

        using var udpClient = new UdpClient();
        if (!config.TestMode)
        {
            udpClient.EnableBroadcast = true;
        }

        udpClient.Client.Bind(new IPEndPoint(bindIp, bindPort));
        _logger.LogInformation("DHCP Listener started on {Address}:{Port} (TestMode: {TestMode})", bindIp, bindPort, config.TestMode);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await udpClient.ReceiveAsync(stoppingToken);
                var requestMessage = _decoder.Decode(result.Buffer);

                using var scope = _scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<IDhcpHandler>();

                var responseMessage = await handler.ProcessMessageAsync(requestMessage, stoppingToken);

                if (responseMessage != null)
                {
                    byte[] responseBytes = _encoder.Encode(responseMessage);
                    IPEndPoint destination = config.TestMode
                        ? result.RemoteEndPoint
                        : new IPEndPoint(IPAddress.Broadcast, result.RemoteEndPoint.Port == 0 ? 68 : result.RemoteEndPoint.Port);

                    await udpClient.SendAsync(responseBytes, responseBytes.Length, destination);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred processing DHCP packet.");
            }
        }
    }
}
