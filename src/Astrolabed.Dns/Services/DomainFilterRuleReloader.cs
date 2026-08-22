// File: src/Astrolabed.Dns/Services/DomainFilterRuleReloader.cs
using Astrolabed.Dns.Filtering;
using Astrolabed.Dns.Options;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Services;

public sealed class DomainFilterRuleReloader : IHostedService, IDisposable
{
    private readonly IOptionsMonitor<DomainFilterRuleOptions> _optionsMonitor;
    private readonly IListLoader _listLoader;
    private readonly IDomainFilterRuleStore _ruleStore;
    private readonly ILogger<DomainFilterRuleReloader> _logger;
    private IDisposable? _onChangeDisposable;

    public DomainFilterRuleReloader(
        IOptionsMonitor<DomainFilterRuleOptions> optionsMonitor,
        IListLoader listLoader,
        IDomainFilterRuleStore ruleStore,
        ILogger<DomainFilterRuleReloader> logger)
    {
        _optionsMonitor = optionsMonitor;
        _listLoader = listLoader;
        _ruleStore = ruleStore;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing Domain Filter Rule Loader HostedService...");

        await LoadAllRulesAsync(cancellationToken).ConfigureAwait(false);

        _onChangeDisposable = _optionsMonitor.OnChange(async (_, _) =>
        {
            _logger.LogInformation("Configuration change detected in DomainFilterRules options. Reloading rule lists...");
            try
            {
                await LoadAllRulesAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reload rules following configuration update.");
            }
        });
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task LoadAllRulesAsync(CancellationToken cancellationToken)
    {
        var options = _optionsMonitor.CurrentValue;
        var aggregatedAllowRules = new List<string>();
        var aggregatedBlockRules = new List<string>();

        // 1. Process Allow List Sources
        if (options.AllowListSources != null)
        {
            foreach (var source in options.AllowListSources)
            {
                try
                {
                    var (allows, blocks) = await _listLoader.LoadRulesAsync(source, cancellationToken).ConfigureAwait(false);
                    aggregatedAllowRules.AddRange(allows);
                    // Standard entries in an explicit allow list default to allow rules
                    aggregatedAllowRules.AddRange(blocks);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load allow list rules from source {Source}", source);
                }
            }
        }

        // 2. Process Block List Sources
        if (options.BlockListSources != null)
        {
            foreach (var source in options.BlockListSources)
            {
                try
                {
                    var (allows, blocks) = await _listLoader.LoadRulesAsync(source, cancellationToken).ConfigureAwait(false);
                    // Explicit exception rules (@@) in blocklists remain allow rules
                    aggregatedAllowRules.AddRange(allows);
                    aggregatedBlockRules.AddRange(blocks);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load block list rules from source {Source}", source);
                }
            }
        }

        _ruleStore.UpdateRules(aggregatedAllowRules, aggregatedBlockRules);
        _logger.LogInformation(
            "Successfully updated IDomainFilterRuleStore. Total aggregated allow rules: {AllowCount}, block rules: {BlockCount}",
            aggregatedAllowRules.Count, aggregatedBlockRules.Count);
    }

    public void Dispose()
    {
        _onChangeDisposable?.Dispose();
    }
}
