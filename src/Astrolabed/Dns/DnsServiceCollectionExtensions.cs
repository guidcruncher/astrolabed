using Astrolabed.Dhcp;
using Astrolabed.Dns.ConditionalForwarding;
using Astrolabed.Dns.Core;
using Astrolabed.Dns.Filtering;
using Astrolabed.Dns.RuleEngine;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dns.Bootstrap;

public static class DnsServiceCollectionExtensions
{

    public static IDnsCache CreateSharedDnsCache(this IServiceCollection services, IConfiguration config)
    {
        var dnsSection = config.GetSection("Dns:Caching");
        services.Configure<CachingOptions>(dnsSection);

        var options = dnsSection.Get<CachingOptions>() ?? new CachingOptions();
        var maxEntries = options.MaxEntries > 0 ? options.MaxEntries : 10000;
        var cleanupIntervalMinutes = options.CleanupIntervalMinutes > 0 ? options.CleanupIntervalMinutes : 1;
        var sharedCache = new DnsCache(maxEntries, TimeSpan.FromMinutes(cleanupIntervalMinutes));
        return sharedCache;
    }

    public static IServiceCollection AddDnsForwarder(this IServiceCollection services, IConfiguration config, IDnsCache sharedCache)
    {
        // Bind configuration into Options Pattern
        services.Configure<ServerOptions>(config);
        services.Configure<DnsForwarderOptions>(config.GetSection("Dns"));

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
