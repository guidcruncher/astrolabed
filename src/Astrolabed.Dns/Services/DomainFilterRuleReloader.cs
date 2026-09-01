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
/// <param name="ruleStore">In-memory rule storage component.</param>
/// <param name="logger">Structured logger instance.</param>
public sealed partial class DomainFilterRuleReloader(
    IOptionsMonitor<DomainFilterRuleOptions> optionsMonitor,
    IListLoader listLoader,
    IDomainFilterRuleStore ruleStore,
    ILogger<DomainFilterRuleReloader> logger) : IHostedService, IDisposable
{
    private readonly IOptionsMonitor<DomainFilterRuleOptions> _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
    private readonly IListLoader _listLoader = listLoader ?? throw new ArgumentNullException(nameof(listLoader));
    private readonly IDomainFilterRuleStore _ruleStore = ruleStore ?? throw new ArgumentNullException(nameof(ruleStore));
    private readonly ILogger<DomainFilterRuleReloader> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private IDisposable? _onChangeDisposable;

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        LogInitializingService(_logger);

        await LoadAllRulesAsync(cancellationToken).ConfigureAwait(false);

        _onChangeDisposable = _optionsMonitor.OnChange((_, _) =>
        {
            LogConfigChangeDetected(_logger);
            _ = ReloadRulesOnConfigChangeAsync();
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
        var aggregatedAllowRules = new List<string>();
        var aggregatedBlockRules = new List<string>();

        // 1. Process Allow List Sources
        if (options.AllowListSources is { Count: > 0 })
        {
            foreach (ListSource source in options.AllowListSources)
            {
                try
                {
                    (IReadOnlyList<string> allows, IReadOnlyList<string> blocks) = await _listLoader
                        .LoadRulesAsync(source, cancellationToken)
                        .ConfigureAwait(false);

                    aggregatedAllowRules.AddRange(allows);
                    // Standard entries in an explicit allow list default to allow rules
                    aggregatedAllowRules.AddRange(blocks);
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
                    (IReadOnlyList<string> allows, IReadOnlyList<string> blocks) = await _listLoader
                        .LoadRulesAsync(source, cancellationToken)
                        .ConfigureAwait(false);

                    // Explicit exception rules (@@) in blocklists remain allow rules
                    aggregatedAllowRules.AddRange(allows);
                    aggregatedBlockRules.AddRange(blocks);
                }
                catch (Exception ex)
                {
                    LogLoadSourceFailed(_logger, ex, source.Path);
                }
            }
        }

        _ruleStore.UpdateRules(aggregatedAllowRules, aggregatedBlockRules);
        LogRulesUpdatedSuccessfully(_logger, aggregatedAllowRules.Count, aggregatedBlockRules.Count);
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
        Message = "Successfully updated IDomainFilterRuleStore. Total aggregated allow rules: {AllowCount}, block rules: {BlockCount}")]
    private static partial void LogRulesUpdatedSuccessfully(ILogger logger, int allowCount, int blockCount);
}
