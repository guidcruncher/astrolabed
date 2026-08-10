using System.Net;

using Astrolabed.Dns;
using Astrolabed.Dns.Core;
using Astrolabed.Dns.Filtering;
using Astrolabed.Dns.RuleEngine;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Astrolabed.Dns.Bootstrap;

public sealed class AstrolabedRuntimeLoader
{
    private readonly IConfiguration _config;

    public AstrolabedRuntimeLoader(IConfiguration config)
    {
        _config = config;
    }

    public async Task LoadAsync(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILogger<AstrolabedRuntimeLoader>>();

        var options = services.GetRequiredService<DnsForwarderOptions>();
        var engine = services.GetRequiredService<RuleEngine.RuleEngine>();

        await LoadHostsAsync(options, engine, logger);
        await LoadBlocklistsAsync(options, engine, logger);
        await LoadAllowlistsAsync(options, engine, logger);
    }

    // ------------------------------------------------------------
    // HOSTS
    // ------------------------------------------------------------
    private async Task LoadHostsAsync(DnsForwarderOptions options, RuleEngine.RuleEngine engine, ILogger logger)
    {
        if (options.HostsFiles?.Any() != true)
        {
            logger.LogWarning("No Hosts files defined in configuration");
            return;
        }

        logger.LogInformation("Loading Hosts files");
        var hostsPaths = options.HostsFiles
            .Select(p => p.StartsWith("file://") ? p[7..] : p);

        var hostsSource = new HostsFileSource(hostsPaths, logger);
        await engine.AddHostsAsync(hostsSource);
        logger.LogInformation($"Loaded {hostsPaths.Count()} Hosts files");
    }

    // ------------------------------------------------------------
    // BLOCKLISTS
    // ------------------------------------------------------------
    private async Task LoadBlocklistsAsync(DnsForwarderOptions options, RuleEngine.RuleEngine engine, ILogger logger)
    {
        if (options.Blocklists?.Any() != true)
        {
            logger.LogWarning("No Block Lists defined in configuration");
            return;
        }

        logger.LogInformation("Loading Block lists");
        var source = CreateSource(options.Blocklists);
        await engine.AddListAsync(source, block: true);
        logger.LogInformation("Finished loading Block lists");
    }

    // ------------------------------------------------------------
    // ALLOWLISTS
    // ------------------------------------------------------------
    private async Task LoadAllowlistsAsync(DnsForwarderOptions options, RuleEngine.RuleEngine engine, ILogger logger)
    {
        if (options.Allowlists?.Any() != true)
        {
            logger.LogWarning("No Allow Lists defined in configuration");
            return;
        }

        logger.LogInformation("Loading Allow lists");
        var source = CreateSource(options.Allowlists);
        await engine.AddListAsync(source, block: false);
        logger.LogInformation("Finished loading Allow lists");
    }

    // ------------------------------------------------------------
    // SOURCE SELECTION (file:// vs URL)
    // ------------------------------------------------------------
    private static IBlocklistSource CreateSource(IEnumerable<string> items)
    {
        var fileItems = items
            .Where(i => i.StartsWith("file://"))
            .Select(i => i.Replace("file://", ""));

        var urlItems = items
            .Where(i => !i.StartsWith("file://"));

        if (fileItems.Any() && urlItems.Any())
        {
            return new CompositeBlocklistSource(new IBlocklistSource[]
            {
                new FileBlocklistSource(fileItems),
                new UrlBlocklistSource(urlItems)
            });
        }

        if (fileItems.Any())
            return new FileBlocklistSource(fileItems);

        return new UrlBlocklistSource(urlItems);
    }
}
