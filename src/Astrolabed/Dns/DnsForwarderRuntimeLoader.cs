using Astrolabed.Dns.Core;
using Astrolabed.Dns.Filtering;
using Astrolabed.Dns.RuleEngine;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Bootstrap;

public sealed class DnsForwarderRuntimeLoader : IDnsForwarderRuntimeLoader
{
    private readonly DnsForwarderOptions _options;
    private readonly Astrolabed.Dns.RuleEngine.RuleEngine _engine;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<DnsForwarderRuntimeLoader> _logger;

    public DnsForwarderRuntimeLoader(
        IOptions<DnsForwarderOptions> options,
        Astrolabed.Dns.RuleEngine.RuleEngine engine,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        ILogger<DnsForwarderRuntimeLoader> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _engine = engine;
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await LoadHostsAsync(cancellationToken).ConfigureAwait(false);
        await LoadBlocklistsAsync(cancellationToken).ConfigureAwait(false);
        await LoadAllowlistsAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task LoadHostsAsync(CancellationToken cancellationToken)
    {
        if (_options.HostsFiles?.Any() != true)
        {
            _logger.LogWarning("No Hosts files defined in configuration");
            return;
        }

        _logger.LogInformation("Loading Hosts files");
        var hostsPaths = _options.HostsFiles
            .Select(p => p.StartsWith("file://", StringComparison.OrdinalIgnoreCase) ? p[7..] : p)
            .ToList();

        var hostsLogger = _loggerFactory.CreateLogger<HostsFileSource>();
        var hostsSource = new HostsFileSource(hostsPaths, hostsLogger);

        await _engine.AddHostsAsync(hostsSource).ConfigureAwait(false);
        _logger.LogInformation("Loaded {Count} Hosts files", hostsPaths.Count);
    }

    private async Task LoadBlocklistsAsync(CancellationToken cancellationToken)
    {
        if (_options.Blocklists?.Any() != true)
        {
            _logger.LogWarning("No Block Lists defined in configuration");
            return;
        }

        _logger.LogInformation("Loading Block lists");
        var source = CreateSource(_options.Blocklists);
        await _engine.AddListAsync(source, block: true).ConfigureAwait(false);
        _logger.LogInformation("Finished loading Block lists");
    }

    private async Task LoadAllowlistsAsync(CancellationToken cancellationToken)
    {
        if (_options.Allowlists?.Any() != true)
        {
            _logger.LogWarning("No Allow Lists defined in configuration");
            return;
        }

        _logger.LogInformation("Loading Allow lists");
        var source = CreateSource(_options.Allowlists);
        await _engine.AddListAsync(source, block: false).ConfigureAwait(false);
        _logger.LogInformation("Finished loading Allow lists");
    }

    private IBlocklistSource CreateSource(IEnumerable<string> items)
    {
        var itemList = items.ToList();

        var fileItems = itemList
            .Where(i => i.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            .Select(i => i[7..])
            .ToList();

        var urlItems = itemList
            .Where(i => !i.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (fileItems.Count > 0 && urlItems.Count > 0)
        {
            var httpClient = _httpClientFactory.CreateClient("BlocklistClient");
            return new CompositeBlocklistSource(new IBlocklistSource[]
            {
                new FileBlocklistSource(fileItems),
                new UrlBlocklistSource(urlItems, httpClient)
            });
        }

        if (fileItems.Count > 0)
            return new FileBlocklistSource(fileItems);

        var client = _httpClientFactory.CreateClient("BlocklistClient");
        return new UrlBlocklistSource(urlItems, client);
    }
}
