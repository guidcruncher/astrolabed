// File: src/Astrolabed.Dns/Extensions/ServiceCollectionExtensions.cs
using System;
using System.Collections.Generic;

using Astrolabed.Dns.Cache;
using Astrolabed.Dns.Filtering;
using Astrolabed.Dns.Models;
using Astrolabed.Dns.Options;
using Astrolabed.Dns.Resolvers;
using Astrolabed.Dns.Services;
using Astrolabed.Dns.Upstream;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Astrolabed.Dns.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAstrolabedDnsEngine(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DnsEngineOptions>(
            configuration.GetSection(DnsEngineOptions.SectionName));

        services.Configure<HostsFileCollectionOptions>(
            configuration.GetSection(HostsFileCollectionOptions.SectionName));

        services.AddHttpClient();

        services.AddSingleton<IDnsCache, DnsCache>();
        services.AddSingleton<IDomainFilter, DummyDomainFilter>();
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

        // Live HostsEntry collection delegation (Resolves empty reference bug)
        services.AddSingleton<IReadOnlyList<HostsEntry>, HostsEntryListWrapper>();

        // Host Record Resolver
        services.AddTransient<IHostRecordResolver, HostRecordResolver>();

        // Upstream Clients
        services.AddTransient<TcpUpstreamDnsClient>();
        services.AddTransient<TcpUpstreamDnsClient>();

        services.AddHttpClient<DoHUpstreamDnsClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(3);
        });

        services.AddSingleton<IUpstreamClientFactory, UpstreamClientFactory>();

        services.AddHostedService<DnsEngine>();

        return services;
    }
}
