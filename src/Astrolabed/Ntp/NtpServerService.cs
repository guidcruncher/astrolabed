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

    private readonly SemaphoreSlim _concurrencySemaphore;
    private UdpClient? _udp;

    public NtpServerService(
        ILogger<NtpServerService> logger,
        INtpRequestHandler handler,
        IOptions<NtpServerOptions> options,
        INtpMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(metrics);

        _logger = logger;
        _handler = handler;
        _options = options.Value;
        _metrics = metrics;

        int maxConcurrency = Math.Max(1, _options.MaxConcurrentRequests);
        _concurrencySemaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
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

                await _concurrencySemaphore.WaitAsync(stoppingToken).ConfigureAwait(false);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ProcessRequestAsync(result, stoppingToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        _concurrencySemaphore.Release();
                    }
                }, CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
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
            var client = _udp;
            if (client is null) return;

            var response = await _handler.HandleAsync(result, client, ct).ConfigureAwait(false);

            _metrics.Sync(new NtpSyncEvent(
                Server: "",
                Delay: TimeSpan.Zero,
                Timestamp: DateTimeOffset.UtcNow,
                ClientIp: result.RemoteEndPoint.Address.ToString(),
                ClientName: string.Empty,
                Offset: response.Offset,
                Success: response.Success));

            if (response.Bytes is { Length: > 0 })
            {
                await client.SendAsync(
                    response.Bytes,
                    response.Bytes.Length,
                    result.RemoteEndPoint).ConfigureAwait(false);
            }
        }
        catch (ObjectDisposedException)
        {
            // Socket closed
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing NTP request from {Remote}",
                result.RemoteEndPoint);

            _metrics.Sync(new NtpSyncEvent(
                Server: "",
                Delay: TimeSpan.Zero,
                Timestamp: DateTimeOffset.UtcNow,
                ClientIp: result.RemoteEndPoint.Address.ToString(),
                ClientName: string.Empty,
                Offset: TimeSpan.Zero,
                Success: false));
        }
    }

    public override void Dispose()
    {
        _udp?.Close();
        _udp?.Dispose();
        _concurrencySemaphore.Dispose();
        base.Dispose();
    }
}
