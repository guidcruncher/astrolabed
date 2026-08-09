using System.Net;

using Astrolabed.Dns.Core;
using Astrolabed.Dns.Filtering;
using Astrolabed.Dns.RuleEngine;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Astrolabed.Dns.Bootstrap;

public static class DnsServiceCollectionExtensions
{
    public static IServiceCollection AddAstrolabed(this IServiceCollection services, IConfiguration config)
    {
        var server = config.Get<ServerOptions>() ?? new ServerOptions();
        var options = server.Dns;

        //
        // Command-line overrides
        //
        if (config["ListenOverride"] is string listen)
        {
            var parts = listen.Split(':');
            options.Listen.Address = parts[0];
            options.Listen.Port = int.Parse(parts[1]);
        }

        if (config["ResolverOverride"] is string resolver)
        {
            var parts = resolver.Split(':');

            // Replace old single DefaultResolver with a list
            options.DefaultResolvers.Clear();
            options.DefaultResolvers.Add(new UpstreamResolverOptions
            {
                Address = parts[0],
                Port = parts.Length > 1 ? int.Parse(parts[1]) : 53,
                Name = "override"
            });
        }

        //
        // Core options
        //
        services.AddSingleton(options);

        //
        // DNS client + caching
        //
        services.AddSingleton<StaticDnsClient>();

        // Add HttpClientFactory so we can create HttpClient instances for DoH upstreams
        services.AddHttpClient();

        // Register the factory that knows how to create Doh vs Udp clients
        services.AddSingleton<IDnsClientFactory, DefaultDnsClientFactory>();

        // Global default IDnsClient (keeps previous behaviour but uses the factory)
        services.AddSingleton<IDnsClient>(sp =>
        {
            var opt = sp.GetRequiredService<AstrolabedOptions>();

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
            var client = factory.Create(selected);

            if (opt.Caching.Enabled)
                client = new CachingDnsClientDecorator(client, opt.Caching.MaxEntries);

            return client;
        });

        //
        // Rule engine + hosts loader
        //
        services.AddSingleton<RuleEngine.RuleEngine>();
        services.AddSingleton<HostsFileSource>();

        //
        // Forwarder + server
        //
        services.AddSingleton<AstrolabedService>();
        services.AddHostedService<DnsServer>();

        return services;
    }
}
