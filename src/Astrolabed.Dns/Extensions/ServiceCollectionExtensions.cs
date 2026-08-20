// File: src/Astrolabed.Dns/Extensions/ServiceCollectionExtensions.cs
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

        services.AddSingleton<IDnsCache, DnsCache>();
        services.AddSingleton<IDomainFilter, DummyDomainFilter>();
        services.AddSingleton<IHostRecordResolver, DummyHostRecordResolver>();
        services.AddSingleton<IPtrResolver, PtrResolver>();

        // Upstream Client Registrations
        services.AddTransient<TcpUpstreamDnsClient>();
        services.AddTransient<DoHUpstreamDnsClient>();
        services.AddTransient<UdpUpstreamDnsClient>();
        services.AddSingleton<IUpstreamClientFactory, UpstreamClientFactory>();

        services.AddHostedService<DnsEngine>();

        return services;
    }
}
