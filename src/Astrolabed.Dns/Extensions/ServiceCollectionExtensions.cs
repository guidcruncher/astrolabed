// File: src/Astrolabed.Dns/Extensions/ServiceCollectionExtensions.cs
using System;
using Astrolabed.Dns.Cache;
using Astrolabed.Dns.Filtering;
using Astrolabed.Dns.Options;
using Astrolabed.Dns.Resolvers;
using Astrolabed.Dns.Services;
using Astrolabed.Dns.Upstream;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Astrolabed.Dns.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAstrolabedDnsEngine(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DnsEngineOptions>(
            configuration.GetSection(DnsEngineOptions.SectionName));

        // Required for IHttpClientFactory and AddHttpClient extension methods
        services.AddHttpClient();

        services.AddSingleton<IDnsCache, DnsCache>();
        services.AddSingleton<IDomainFilter, DummyDomainFilter>();
        services.AddSingleton<IHostRecordResolver, DummyHostRecordResolver>();
        services.AddSingleton<IPtrResolver, PtrResolver>();

        // Hosts File Reader
        services.AddHttpClient<IHostsFileReader, HostsFileReader>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        // Upstream Clients
        services.AddTransient<UdpUpstreamDnsClient>();
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
