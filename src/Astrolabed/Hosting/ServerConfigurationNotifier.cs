using Astrolabed;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Hosting;

public sealed class ServerConfigurationNotifier : IHostedService, IDisposable
{
    private readonly IOptionsMonitor<ServerOptions> _optionsMonitor;
    private readonly ILogger<ServerConfigurationNotifier> _logger;
    private IDisposable? _changeTokenSubscription;

    public ServerConfigurationNotifier(
        IOptionsMonitor<ServerOptions> optionsMonitor,
        ILogger<ServerConfigurationNotifier> logger)
    {
        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _changeTokenSubscription = _optionsMonitor.OnChange(OnConfigurationChanged);
        _logger.LogInformation("Subscribed to live configuration changes.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping configuration notifier service.");
        return Task.CompletedTask;
    }

    private void OnConfigurationChanged(ServerOptions newOptions, string? name)
    {
        _logger.LogInformation(
            "Configuration changed dynamically. Section Name: {SectionName}, Current Settings: {@Options}",
            name ?? "Default",
            newOptions);

        // React to specific configuration updates here (e.g., reconfigure clients, flush caches)
    }

    public void Dispose()
    {
        _changeTokenSubscription?.Dispose();
    }
}
