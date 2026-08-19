// File: src/Astrolabed.Dns/Extensions/ServiceCollectionExtensions.cs
using Astrolabed.Dns.Cache;
using Astrolabed.Dns.Filtering;
using Astrolabed.Dns.Options;
using Astrolabed.Dns.Resolvers;
using Astrolabed.Dns.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Astrolabed.Dns.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAstrolabedDnsEngine(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Configure Options Pattern
        services.Configure<DnsEngineOptions>(
            configuration.GetSection(DnsEngineOptions.SectionName));

        // 2. Register Cache Service
        services.AddSingleton<IDnsCache, DnsCache>();

        // 3. Register Domain Filter & Resolvers
        services.AddSingleton<IDomainFilter, DummyDomainFilter>();
        services.AddSingleton<IHostRecordResolver, DummyHostRecordResolver>();
        services.AddSingleton<IPtrResolver, DummyPtrResolver>();

        // 4. Register Background Engine Service
        services.AddHostedService<DnsEngine>();

        return services;
    }
}
