// File: src/Astrolabed.Dns/Resolvers/HostsManager.cs
using System.Collections.Frozen;
using System.Net;

using Astrolabed.Dns.Models;
using Astrolabed.Dns.Options;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Resolvers;

/// <summary>
/// Manages asynchronous loading, dynamic configuration reloading, and merging of local hosts file entries.
/// </summary>
public sealed partial class HostsManager : IHostsManager, IHostedService, IDisposable
{
    /// <summary>
    /// Service responsible for reading and parsing raw hosts files from local disk or network sources.
    /// </summary>
    private readonly IHostsFileReader _hostsFileReader;

    /// <summary>
    /// Monitor instance tracking dynamic updates to <see cref="HostsFileCollectionOptions"/>.
    /// </summary>
    private readonly IOptionsMonitor<HostsFileCollectionOptions> _optionsMonitor;

    /// <summary>
    /// Structured logger instance for diagnostic events and errors.
    /// </summary>
    private readonly ILogger<HostsManager> _logger;

    /// <summary>
    /// Subscription token for change notifications on options updates.
    /// </summary>
    private readonly IDisposable? _optionsChangeListener;

    /// <summary>
    /// Synchronization primitive ensuring thread-safe, mutually exclusive reloads.
    /// </summary>
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// Volatile backing store holding the current thread-safe list snapshot of loaded hosts entries.
    /// </summary>
    private IReadOnlyList<HostsEntry> _entries = Array.Empty<HostsEntry>();

    /// <summary>
    /// Volatile backing store holding the current thread-safe frozen lookup dictionary of host entries.
    /// </summary>
    private FrozenDictionary<string, HostsEntry> _lookup = FrozenDictionary<string, HostsEntry>.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="HostsManager"/> class with required dependencies and options change tracking.
    /// </summary>
    /// <param name="hostsFileReader">Reader service for parsing hosts file sources.</param>
    /// <param name="optionsMonitor">Options monitor for tracking dynamic configuration updates.</param>
    /// <param name="logger">Structured logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is <c>null</c>.</exception>
    public HostsManager(
        IHostsFileReader hostsFileReader,
        IOptionsMonitor<HostsFileCollectionOptions> optionsMonitor,
        ILogger<HostsManager> logger)
    {
        _hostsFileReader = hostsFileReader ?? throw new ArgumentNullException(nameof(hostsFileReader));
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _optionsChangeListener = _optionsMonitor.OnChange((options, name) =>
        {
            LogConfigurationChanged(_logger, name ?? "default");
            _ = Task.Run(async () =>
            {
                try
                {
                    await ReloadAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    LogConfigurationReloadFailed(_logger, ex);
                }
            });
        });
    }

    /// <inheritdoc />
    public IReadOnlyList<HostsEntry> Entries => Volatile.Read(ref _entries);

    /// <inheritdoc />
    public FrozenDictionary<string, HostsEntry> Lookup => Volatile.Read(ref _lookup);

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        LogInitializingHostsManager(_logger);
        await ReloadAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public async Task ReloadAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            HostsFileCollectionOptions options = _optionsMonitor.CurrentValue;
            if (options.Sources is null || options.Sources.Count == 0)
            {
                LogNoSourcesConfigured(_logger);
                Volatile.Write(ref _entries, Array.Empty<HostsEntry>());
                Volatile.Write(ref _lookup, FrozenDictionary<string, HostsEntry>.Empty);
                return;
            }

            var aggregatedMap = new Dictionary<string, HashSet<IPAddress>>(StringComparer.OrdinalIgnoreCase);

            foreach (string source in options.Sources)
            {
                if (string.IsNullOrWhiteSpace(source))
                {
                    continue;
                }

                try
                {
                    LogLoadingSource(_logger, source);
                    IReadOnlyDictionary<string, IReadOnlyList<IPAddress>> fileEntries = await _hostsFileReader.ReadHostsAsync(source, ct).ConfigureAwait(false);

                    foreach ((string hostname, IReadOnlyList<IPAddress> addresses) in fileEntries)
                    {
                        if (!aggregatedMap.TryGetValue(hostname, out HashSet<IPAddress>? addressSet))
                        {
                            addressSet = new HashSet<IPAddress>();
                            aggregatedMap[hostname] = addressSet;
                        }

                        foreach (IPAddress addr in addresses)
                        {
                            addressSet.Add(addr);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogFailedToLoadSource(_logger, ex, source);
                }
            }

            List<HostsEntry> mergedEntries = aggregatedMap
                .Select(kvp => new HostsEntry(kvp.Key, kvp.Value.ToList()))
                .ToList();

            var lookupDict = mergedEntries.ToDictionary(
                e => e.Hostname.Trim().TrimEnd('.'),
                e => e,
                StringComparer.OrdinalIgnoreCase);

            Volatile.Write(ref _entries, mergedEntries);
            Volatile.Write(ref _lookup, lookupDict.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase));

            LogMergedAndDeduplicated(_logger, mergedEntries.Count);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _optionsChangeListener?.Dispose();
        _lock.Dispose();
    }

    [LoggerMessage(
        EventId = 501,
        Level = LogLevel.Information,
        Message = "Hosts configuration changed ({Name}). Triggering dynamic reload...")]
    private static partial void LogConfigurationChanged(ILogger logger, string name);

    [LoggerMessage(
        EventId = 502,
        Level = LogLevel.Error,
        Message = "Failed to reload hosts configuration following change event.")]
    private static partial void LogConfigurationReloadFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 503,
        Level = LogLevel.Information,
        Message = "Initializing HostsManager and loading hosts files...")]
    private static partial void LogInitializingHostsManager(ILogger logger);

    [LoggerMessage(
        EventId = 504,
        Level = LogLevel.Warning,
        Message = "No hosts file sources configured in HostsFileCollectionOptions.")]
    private static partial void LogNoSourcesConfigured(ILogger logger);

    [LoggerMessage(
        EventId = 505,
        Level = LogLevel.Information,
        Message = "Loading hosts entries from {Source}")]
    private static partial void LogLoadingSource(ILogger logger, string source);

    [LoggerMessage(
        EventId = 506,
        Level = LogLevel.Error,
        Message = "Failed to load hosts source from {Source}")]
    private static partial void LogFailedToLoadSource(ILogger logger, Exception exception, string source);

    [LoggerMessage(
        EventId = 507,
        Level = LogLevel.Information,
        Message = "Successfully merged and deduplicated {Count} unique hostnames.")]
    private static partial void LogMergedAndDeduplicated(ILogger logger, int count);
}
