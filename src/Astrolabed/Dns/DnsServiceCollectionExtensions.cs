using System;

using Astrolabed;
using Astrolabed.Dhcp;
using Astrolabed.Dns.ConditionalForwarding;
using Astrolabed.Dns.Core;
using Astrolabed.Dns.Filtering;
using Astrolabed.Dns.RuleEngine;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Bootstrap;

public static class DnsServiceCollectionExtensions
{

    private static readonly Lock SyncLock = new();
    private static DnsCache? _processSharedCache;

    public static IServiceCollection AddSharedDnsCache(
        this IServiceCollection services,
        IConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(config);

        // 1. Bind configuration options from the "Dns:Caching" section per-host
        var dnsSection = config.GetSection("Dns:Caching");
        services.Configure<CachingOptions>(options =>
        {
            var cachingOptions = dnsSection.Get<CachingOptions>();
            if (cachingOptions != null)
            {
                if (cachingOptions.MaxEntries > 0)
                {
                    options.MaxEntries = cachingOptions.MaxEntries;
                }

                if (cachingOptions.CleanupIntervalMinutes > 0)
                {
                    options.CleanupIntervalMinutes = cachingOptions.CleanupIntervalMinutes;
                }
            }
        });

        // 2. Register or retrieve the cross-IHost shared singleton instance
        services.AddSingleton<IDnsCache>(sp =>
        {
            if (_processSharedCache != null)
            {
                return _processSharedCache;
            }

            lock (SyncLock)
            {
                if (_processSharedCache == null)
                {
                    var options = sp.GetRequiredService<IOptions<CachingOptions>>();
                    var logger = sp.GetRequiredService<ILogger<DnsCache>>();
                    var timeProvider = sp.GetService<TimeProvider>();

                    _processSharedCache = new DnsCache(options, logger, timeProvider);
                }

                return _processSharedCache;
            }
        });

        return services;
    }

    public static IServiceCollection AddDnsForwarder(this IServiceCollection services, IConfiguration config, IDnsCache sharedCache)
    {
        // Bind configuration into Options Pattern
        services.Configure<ServerOptions>(config);
        services.Configure<DnsForwarderOptions>(config.GetSection(DnsForwarderOptions.SectionName));

        // Command-line/dynamic post-configuration overrides
        services.PostConfigure<DnsForwarderOptions>(options =>
        {
            if (config["ListenOverride"] is string listen)
            {
                var parts = listen.Split(':');
                options.Listen.Address = parts[0];
                options.Listen.Port = int.Parse(parts[1]);
            }

            if (config["ResolverOverride"] is string resolver)
            {
                var parts = resolver.Split(':');
                options.DefaultResolvers.Clear();
                options.DefaultResolvers.Add(new UpstreamResolverOptions
                {
                    Address = parts[0],
                    Port = parts.Length > 1 ? int.Parse(parts[1]) : 53,
                    Name = "override"
                });
            }
        });

        // HTTP Client infrastructure
        services.AddHttpClient();
        services.AddHttpClient("BlocklistClient");

        services.AddSingleton<IDnsCache>(sharedCache);
        // Local DHCP lease reader
        services.AddTransient<IDhcpLeaseReader, DhcpLeaseReader>();

        // DNS Infrastructure Clients
        services.AddSingleton<IDnsClientFactory, DefaultDnsClientFactory>();

        services.AddSingleton<IConditionalDnsForwarder, ConditionalDnsForwarder>();

        // Register default IDnsClient using IOptions
        services.AddSingleton<IDnsClient>(sp =>
        {
            var opt = sp.GetRequiredService<IOptions<DnsForwarderOptions>>().Value;

            var resolvers = opt.DefaultResolvers.Count > 0
                ? opt.DefaultResolvers
                : new List<UpstreamResolverOptions>
                {
                    new UpstreamResolverOptions
                    {
                        Address = "8.8.8.8",
                        Port = 53,
                        Name = "fallback-default"
                    }
                };

            var selected = resolvers[0];
            var factory = sp.GetRequiredService<IDnsClientFactory>();
            IDnsClient client = factory.Create(selected);

            if (opt.Caching.Enabled)
            {
                client = new CachingDnsClientDecorator(client, opt.Caching.MaxEntries);
            }

            return client;
        });

        // Core Rule Engine and Loaders
        services.AddSingleton<RuleEngine.RuleEngine>();
        services.AddTransient<IHostsFileSource, HostsFileSource>();
        services.AddSingleton<IDnsForwarderRuntimeLoader, DnsForwarderRuntimeLoader>();

        // DNS Forwarding & Hosting
        services.AddSingleton<DnsForwarderService>();
        services.AddHostedService<DnsServer>();

        return services;
    }
}
