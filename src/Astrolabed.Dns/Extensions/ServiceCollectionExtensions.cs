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
    /// Registers the core Astrolabed DNS engine services, resolvers, cache, network listeners, event listeners, and background tasks.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configuration">Application configuration provider.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configuration"/> is <c>null</c>.</exception>
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

        // Core Caches & Filter Rule Stores
        services.AddSingleton<IDnsCache, DnsCache>();

        services.AddSingleton<DomainFilterRuleStore>();
        services.AddSingleton<IDomainFilterRuleStore>(sp => sp.GetRequiredService<DomainFilterRuleStore>());
        services.AddSingleton<IReadOnlyDomainFilterRules>(sp => sp.GetRequiredService<DomainFilterRuleStore>());

        // Evaluation Engine
        services.AddSingleton<IDomainFilter, DomainFilter>();

        // List Loaders
        services.AddHttpClient<IListLoader, AdGuardListLoader>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Options-driven Rule Loader
        services.AddAstrolabedDomainFilterRuleLoader(configuration);

        services.AddSingleton<IPtrResolver, PtrResolver>();

        // Hosts File Reader & Manager
        services.AddHttpClient<IHostsFileReader, HostsFileReader>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddSingleton<HostsManager>();
        services.AddSingleton<IHostsManager>(sp => sp.GetRequiredService<HostsManager>());
        services.AddHostedService(sp => sp.GetRequiredService<HostsManager>());

        services.AddSingleton<IReadOnlyList<HostsEntry>, HostsEntryListWrapper>();

        // Host Record Resolvers & Upstream Clients
        services.AddTransient<IHostRecordResolver, HostRecordResolver>();
        services.AddTransient<UdpUpstreamDnsClient>();
        services.AddTransient<TcpUpstreamDnsClient>();

        services.AddHttpClient<DoHUpstreamDnsClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(3);
        });

        services.AddSingleton<IUpstreamClientFactory, UpstreamClientFactory>();

        // Client Name Resolution & Network Scanning (Scoped for clean database access inside jobs/scopes)
        services.AddScoped<INetworkScannerService, NetworkScannerService>();
        services.AddSingleton<IClientNameResolver, ClientNameResolver>();

        // Event Bus Listeners
        // services.AddScoped<IEventListener<DnsResponseEvent>, DnsResponseListener>();

        // Core Query Processing & Transport Listeners Pattern
        services.AddSingleton<IDnsQueryProcessor, DnsQueryProcessor>();
        services.AddSingleton<IDnsListener, DnsUdpListener>();
        services.AddSingleton<IDnsListener, DnsTcpListener>();

        // Scheduled Background Jobs & Engine
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configuration"/> is <c>null</c>.</exception>
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

