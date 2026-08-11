using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Events;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Ntp;

public sealed class NtpServerService : BackgroundService
{
    private readonly ILogger<NtpServerService> _logger;
    private readonly INtpRequestHandler _handler;
    private readonly NtpServerOptions _options;
    private readonly INtpMetrics _metrics;

    private UdpClient? _udp;

    public NtpServerService(
        ILogger<NtpServerService> logger,
        INtpRequestHandler handler,
        IOptions<NtpServerOptions> options,
        INtpMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(options);

        _logger = logger;
        _handler = handler;
        _options = options.Value;
        _metrics = metrics;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!IPAddress.TryParse(_options.ListenAddress, out var address))
        {
            _logger.LogCritical("Invalid IP Address in NTP ListenAddress. Cannot initialise NTP Service");
            return;
        }

        _udp = new UdpClient(address.AddressFamily);
        _udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

        if (_options.BufferSize > 0)
        {
            _udp.Client.ReceiveBufferSize = _options.BufferSize;
            _udp.Client.SendBufferSize = _options.BufferSize;
        }

        var endpoint = new IPEndPoint(address, _options.Port);
        _udp.Client.Bind(endpoint);

        _logger.LogInformation("NTP server listening on {Endpoint}", endpoint);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await _udp.ReceiveAsync(stoppingToken).ConfigureAwait(false);
                _ = ProcessRequestAsync(result, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving UDP packet");
            }
        }
    }

    private async Task ProcessRequestAsync(UdpReceiveResult result, CancellationToken ct)
    {
        try
        {
            var response = await _handler.HandleAsync(result, _udp!, ct).ConfigureAwait(false);

            _metrics.Sync(new NtpSyncEvent(
                Timestamp: DateTime.UtcNow,
                ClientIp: result.RemoteEndPoint.Address,
                ClientName: null,
                Offset: response.Offset,
                Success: response.Success));

            if (response.Bytes is { Length: > 0 } && _udp is not null)
            {
                await _udp.SendAsync(
                    response.Bytes,
                    response.Bytes.Length,
                    result.RemoteEndPoint).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing NTP request from {Remote}",
                result.RemoteEndPoint);

            _metrics.Sync(new NtpSyncEvent(
                Timestamp: DateTime.UtcNow,
                ClientIp: result.RemoteEndPoint.Address,
                ClientName: null,
                Offset: TimeSpan.Zero,
                Success: false));
        }
    }

    public override void Dispose()
    {
        _udp?.Close();
        _udp?.Dispose();
        base.Dispose();
    }
}
