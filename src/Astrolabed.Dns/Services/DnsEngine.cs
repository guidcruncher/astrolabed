// File: src/Astrolabed.Dns/Services/DnsEngine.cs
using System.Net;

using Astrolabed.Dns.Options;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Services;

public sealed class DnsEngine : BackgroundService
{
    private readonly IOptionsMonitor<DnsEngineOptions> _optionsMonitor;
    private readonly IEnumerable<IDnsListener> _listeners;
    private readonly ILogger<DnsEngine> _logger;

    public DnsEngine(
        IOptionsMonitor<DnsEngineOptions> optionsMonitor,
        IEnumerable<IDnsListener> listeners,
        ILogger<DnsEngine> logger)
    {
        _optionsMonitor = optionsMonitor;
        _listeners = listeners;
        _logger = logger;

        var initialOptions = _optionsMonitor.CurrentValue;
        ThreadPool.SetMinThreads(initialOptions.ProcessingThreads * 2, initialOptions.ProcessingThreads * 2);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield execution immediately back to Host.StartAsync so application startup completes without hanging
        await Task.Yield();

        var options = _optionsMonitor.CurrentValue;
        var address = string.IsNullOrEmpty(options.ListenAddress.Address)
            ? IPAddress.Any
            : IPAddress.Parse(options.ListenAddress.Address);
        int port = options.ListenAddress.Port;

        var listenerList = _listeners.ToList();
        _logger.LogInformation("Starting DNS Engine with {Count} transport listeners on {Address}#{Port}...", listenerList.Count, address.ToString(), port);

        var listenTasks = listenerList.Select(listener => Task.Run(async () =>
        {
            try
            {
                await listener.ListenAsync(address, port, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected cancellation during shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transport listener {ListenerType} encountered an unhandled exception.", listener.GetType().Name);
            }
        }, stoppingToken)).ToList();

        await Task.WhenAll(listenTasks).ConfigureAwait(false);
    }
}
