using System.Net;

using Astrolabed.Dns.Options;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Services;

/// <summary>
/// Hosted background service responsible for orchestrating lifecycle management and transport listeners for the DNS Engine.
/// </summary>
/// <param name="optionsMonitor">Monitored DNS engine configuration options.</param>
/// <param name="listeners">Registered DNS protocol transport listeners (e.g., UDP, TCP, DoH).</param>
/// <param name="logger">Structured logger instance.</param>
public sealed partial class DnsEngine(
    IOptionsMonitor<DnsEngineOptions> optionsMonitor,
    IEnumerable<IDnsListener> listeners,
    ILogger<DnsEngine> logger) : BackgroundService
{
    private readonly IOptionsMonitor<DnsEngineOptions> _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
    private readonly IDnsListener[] _listeners = listeners?.ToArray() ?? throw new ArgumentNullException(nameof(listeners));
    private readonly ILogger<DnsEngine> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield execution back to Host.StartAsync to permit non-blocking host bootstrap
        await Task.Yield();

        DnsEngineOptions options = _optionsMonitor.CurrentValue;

        IPAddress address = string.IsNullOrWhiteSpace(options.ListenAddress.Address)
            ? IPAddress.Any
            : IPAddress.Parse(options.ListenAddress.Address);

        int port = options.ListenAddress.Port;

        LogStartingEngine(_logger, _listeners.Length, address, port);

        var listenTasks = new Task[_listeners.Length];
        for (int i = 0; i < _listeners.Length; i++)
        {
            IDnsListener listener = _listeners[i];
            listenTasks[i] = StartListenerAsync(listener, address, port, stoppingToken);
        }

        await Task.WhenAll(listenTasks).ConfigureAwait(false);
    }

    private async Task StartListenerAsync(IDnsListener listener, IPAddress address, int port, CancellationToken stoppingToken)
    {
        try
        {
            await listener.ListenAsync(address, port, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected cancellation during service shutdown
        }
        catch (Exception ex)
        {
            LogListenerError(_logger, ex, listener.GetType().Name);
        }
    }

    [LoggerMessage(
        EventId = 101,
        Level = LogLevel.Information,
        Message = "Starting DNS Engine with {Count} transport listeners on {Address}#{Port}...")]
    private static partial void LogStartingEngine(ILogger logger, int count, IPAddress address, int port);

    [LoggerMessage(
        EventId = 102,
        Level = LogLevel.Error,
        Message = "Transport listener {ListenerType} encountered an unhandled exception.")]
    private static partial void LogListenerError(ILogger logger, Exception exception, string listenerType);
}
