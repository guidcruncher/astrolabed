using System;
using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<Task, bool> _activeTasks = new();
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

                var task = ProcessRequestAsync(result, stoppingToken);
                _activeTasks.TryAdd(task, true);

                _ = task.ContinueWith(
                    t =>
                    {
                        _activeTasks.TryRemove(t, out _);
                        _concurrencySemaphore.Release();
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
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

        await CompleteActiveTasksAsync().ConfigureAwait(false);
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

    private async Task CompleteActiveTasksAsync()
    {
        var tasks = _activeTasks.Keys;
        if (tasks.Count > 0)
        {
            _logger.LogInformation("Waiting for {Count} active NTP requests to complete...", tasks.Count);
            await Task.WhenAll(tasks).ConfigureAwait(false);
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
