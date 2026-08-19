// File: src/Astrolabed.Dns/Extensions/ServiceCollectionExtensions.cs
using Astrolabed.Dns.Cache;
using Astrolabed.Dns.Options;
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
        // 1. Configure Microsoft Options & Options Monitor Extension
        services.Configure<DnsEngineOptions>(
            configuration.GetSection(DnsEngineOptions.SectionName));

        // 2. Register Lock-Free Singleton Cache Service
        services.AddSingleton<ILockFreeDnsCache, LockFreeDnsCache>();

        // 3. Register Hosted Engine Background Service
        services.AddHostedService<OptimizedDnsEngine>();

        return services;
    }
}
