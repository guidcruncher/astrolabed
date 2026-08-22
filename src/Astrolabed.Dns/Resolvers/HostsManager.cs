// File: src/Astrolabed.Dns/Resolvers/HostsManager.cs
using System.Net;

using Astrolabed.Dns.Models;
using Astrolabed.Dns.Options;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Resolvers;

public sealed class HostsManager : IHostsManager, IHostedService, IDisposable
{
    private readonly IHostsFileReader _hostsFileReader;
    private readonly IOptionsMonitor<HostsFileCollectionOptions> _optionsMonitor;
    private readonly ILogger<HostsManager> _logger;
    private readonly IDisposable? _optionsChangeListener;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private IReadOnlyList<HostsEntry> _entries = Array.Empty<HostsEntry>();

    public IReadOnlyList<HostsEntry> Entries => _entries;

    public HostsManager(
        IHostsFileReader hostsFileReader,
        IOptionsMonitor<HostsFileCollectionOptions> optionsMonitor,
        ILogger<HostsManager> logger)
    {
        _hostsFileReader = hostsFileReader;
        _optionsMonitor = optionsMonitor;
        _logger = logger;

        _optionsChangeListener = _optionsMonitor.OnChange((options, name) =>
        {
            _logger.LogInformation("Hosts configuration changed ({Name}). Triggering dynamic reload...", name);
            _ = ReloadAsync();
        });
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing HostsManager and loading hosts files...");
        await ReloadAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task ReloadAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var options = _optionsMonitor.CurrentValue;
            if (options.Sources == null || options.Sources.Count == 0)
            {
                _logger.LogWarning("No hosts file sources configured in HostsFileCollectionOptions.");
                _entries = Array.Empty<HostsEntry>();
                return;
            }

            var aggregatedMap = new Dictionary<string, HashSet<IPAddress>>(StringComparer.OrdinalIgnoreCase);

            foreach (var source in options.Sources)
            {
                if (string.IsNullOrWhiteSpace(source)) continue;

                try
                {
                    _logger.LogInformation("Loading hosts entries from {Source}", source);
                    var fileEntries = await _hostsFileReader.ReadHostsAsync(source, ct).ConfigureAwait(false);

                    foreach (var (hostname, addresses) in fileEntries)
                    {
                        if (!aggregatedMap.TryGetValue(hostname, out var addressSet))
                        {
                            addressSet = new HashSet<IPAddress>();
                            aggregatedMap[hostname] = addressSet;
                        }

                        foreach (var addr in addresses)
                        {
                            addressSet.Add(addr);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load hosts source from {Source}", source);
                }
            }

            var mergedEntries = aggregatedMap
                .Select(kvp => new HostsEntry(kvp.Key, kvp.Value.ToList()))
                .ToList();

            _entries = mergedEntries;
            _logger.LogInformation("Successfully merged and deduplicated {Count} unique hostnames.", _entries.Count);
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        _optionsChangeListener?.Dispose();
        _lock.Dispose();
    }
}
