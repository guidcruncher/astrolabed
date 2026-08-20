// File: src/Astrolabed.Dns/Services/DnsEngine.cs
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

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
        var options = _optionsMonitor.CurrentValue;
        var address = string.IsNullOrEmpty(options.ListenAddress.Address) ? IPAddress.Any : IPAddress.Parse(options.ListenAddress.Address);
        int port = options.ListenAddress.Port;

        _logger.LogInformation("Starting DNS Engine with {Count} transport listeners on {Address}#{Port}...", _listeners.Count(), address.ToString(), port);

        var listenTasks = _listeners.Select(listener => listener.ListenAsync(address, port, stoppingToken));

        await Task.WhenAll(listenTasks).ConfigureAwait(false);
    }
}

