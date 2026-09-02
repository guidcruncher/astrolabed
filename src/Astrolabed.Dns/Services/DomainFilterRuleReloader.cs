// File: src/Astrolabed.Dns/Services/DomainFilterRuleReloader.cs
using Astrolabed.Dns.Filtering;
using Astrolabed.Dns.Options;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Services;

/// <summary>
/// Hosted background service responsible for fetching, compiling, and updating domain filtering rules dynamically on configuration updates.
/// </summary>
/// <param name="optionsMonitor">Monitored domain filter options.</param>
/// <param name="listLoader">List loading engine for remote/local sources.</param>
/// <param name="logger">Structured logger instance.</param>
public sealed partial class DomainFilterRuleReloader(
    IOptionsMonitor<DomainFilterRuleOptions> optionsMonitor,
    IListLoader listLoader,
    ILogger<DomainFilterRuleReloader> logger) : IHostedService, IDisposable
{
    private readonly IOptionsMonitor<DomainFilterRuleOptions> _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
    private readonly IListLoader _listLoader = listLoader ?? throw new ArgumentNullException(nameof(listLoader));
    private readonly ILogger<DomainFilterRuleReloader> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private IDisposable? _onChangeDisposable;

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        LogInitializingService(_logger);

        await LoadAllRulesAsync(cancellationToken).ConfigureAwait(false);

        _onChangeDisposable = _optionsMonitor.OnChange(async (_, _) =>
        {
            LogConfigChangeDetected(_logger);
            await ReloadRulesOnConfigChangeAsync().ConfigureAwait(false);
        });
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task ReloadRulesOnConfigChangeAsync()
    {
        try
        {
            await LoadAllRulesAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogReloadFailed(_logger, ex);
        }
    }

    private async Task LoadAllRulesAsync(CancellationToken cancellationToken)
    {
        DomainFilterRuleOptions options = _optionsMonitor.CurrentValue;

        // 1. Process Allow List Sources
        if (options.AllowListSources is { Count: > 0 })
        {
            foreach (ListSource source in options.AllowListSources)
            {
                try
                {
                    await _listLoader.LoadAndApplyListAsync(source, cancellationToken).ConfigureAwait(false);
                    LogRulesUpdatedSuccessfully(_logger, source.Id, source.Path);
                }
                catch (Exception ex)
                {
                    LogLoadSourceFailed(_logger, ex, source.Path);
                }
            }
        }

        // 2. Process Block List Sources
        if (options.BlockListSources is { Count: > 0 })
        {
            foreach (ListSource source in options.BlockListSources)
            {
                try
                {
                    await _listLoader.LoadAndApplyListAsync(source, cancellationToken).ConfigureAwait(false);
                    LogRulesUpdatedSuccessfully(_logger, source.Id, source.Path);
                }
                catch (Exception ex)
                {
                    LogLoadSourceFailed(_logger, ex, source.Path);
                }
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _onChangeDisposable?.Dispose();
    }

    [LoggerMessage(
        EventId = 401,
        Level = LogLevel.Information,
        Message = "Initializing Domain Filter Rule Loader HostedService...")]
    private static partial void LogInitializingService(ILogger logger);

    [LoggerMessage(
        EventId = 402,
        Level = LogLevel.Information,
        Message = "Configuration change detected in DomainFilterRules options. Reloading rule lists...")]
    private static partial void LogConfigChangeDetected(ILogger logger);

    [LoggerMessage(
        EventId = 403,
        Level = LogLevel.Error,
        Message = "Failed to reload rules following configuration update.")]
    private static partial void LogReloadFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 404,
        Level = LogLevel.Error,
        Message = "Failed to load list rules from source {Source}")]
    private static partial void LogLoadSourceFailed(ILogger logger, Exception exception, string source);

    [LoggerMessage(
        EventId = 405,
        Level = LogLevel.Information,
        Message = "Successfully updated IFilterRuleStore for ListId {ListId} from {Source}")]
    private static partial void LogRulesUpdatedSuccessfully(ILogger logger, int listId, string source);
}
