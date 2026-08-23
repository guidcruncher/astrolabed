// File: src/Astrolabed.Dns/Extensions/ServiceCollectionExtensions.cs
using Astrolabed.Core.Scheduler;
using Astrolabed.Dns.Cache;
using Astrolabed.Dns.Filtering;
using Astrolabed.Dns.Jobs;
using Astrolabed.Dns.Models;
using Astrolabed.Dns.Options;
using Astrolabed.Dns.Resolvers;
using Astrolabed.Dns.Services;
using Astrolabed.Dns.Upstream;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Astrolabed.Dns.Extensions;

/// <summary>
/// Extension methods for registering Astrolabed DNS engine services into an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core Astrolabed DNS engine services, resolvers, cache, network listeners, and background tasks.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configuration">Application configuration provider.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>

    public static IServiceCollection AddDnsServer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Options Binding with Data Annotation Validation & Fast Startup Check
        services.AddOptions<NetworkScannerOptions>()
            .Bind(configuration.GetSection(NetworkScannerOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<DnsEngineOptions>()
            .Bind(configuration.GetSection(DnsEngineOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<HostsFileCollectionOptions>()
            .Bind(configuration.GetSection(HostsFileCollectionOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient();

        services.AddSingleton<IDnsCache, DnsCache>();

        // Domain Filter Rule Store registration (Handles deduplication, storage, and rule mutation)
        services.AddSingleton<DomainFilterRuleStore>();
        services.AddSingleton<IDomainFilterRuleStore>(sp => sp.GetRequiredService<DomainFilterRuleStore>());
        services.AddSingleton<IReadOnlyDomainFilterRules>(sp => sp.GetRequiredService<DomainFilterRuleStore>());

        // Domain Filter Evaluation Engine
        services.AddSingleton<IDomainFilter, DomainFilter>();

        // List Loader registration
        services.AddHttpClient<IListLoader, AdGuardListLoader>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Register Options-driven Domain Filter Rule Loader Service
        services.AddAstrolabedDomainFilterRuleLoader(configuration);

        services.AddSingleton<IPtrResolver, PtrResolver>();

        // Hosts File Reader
        services.AddHttpClient<IHostsFileReader, HostsFileReader>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        // Register HostsManager Singleton & HostedService
        services.AddSingleton<HostsManager>();
        services.AddSingleton<IHostsManager>(sp => sp.GetRequiredService<HostsManager>());
        services.AddHostedService<HostsManager>(sp => sp.GetRequiredService<HostsManager>());

        // Live HostsEntry collection delegation
        services.AddSingleton<IReadOnlyList<HostsEntry>, HostsEntryListWrapper>();

        // Host Record Resolver
        services.AddTransient<IHostRecordResolver, HostRecordResolver>();

        // Upstream Clients
        services.AddTransient<UdpUpstreamDnsClient>();
        services.AddTransient<TcpUpstreamDnsClient>();

        services.AddHttpClient<DoHUpstreamDnsClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(3);
        });

        services.AddSingleton<IUpstreamClientFactory, UpstreamClientFactory>();

        // Client name resolver & network scanner
        services.AddTransient<INetworkScannerService, NetworkScannerService>();
        services.AddSingleton<IClientNameResolver, ClientNameResolver>();

        // Core Query Processing & Transport Listeners Pattern
        services.AddSingleton<IDnsQueryProcessor, DnsQueryProcessor>();
        services.AddSingleton<IDnsListener, DnsUdpListener>();
        services.AddSingleton<IDnsListener, DnsTcpListener>();

        // Scheduled background jobs
        services.AddJobScheduler();
        services.AddScheduledJob<NetworkScanJob>();

        services.AddHostedService<DnsEngine>();

        return services;
    }

    /// <summary>
    /// Registers the domain filter rule loader hosted service and options.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configuration">Application configuration provider.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
    public static IServiceCollection AddAstrolabedDomainFilterRuleLoader(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<DomainFilterRuleOptions>()
            .Bind(configuration.GetSection(DomainFilterRuleOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHostedService<DomainFilterRuleReloader>();

        return services;
    }
}
