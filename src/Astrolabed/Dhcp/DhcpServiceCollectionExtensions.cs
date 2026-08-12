using System.Net;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Dhcp.Bootstrap;

public static class DhcpServiceCollectionExtensions
{
    public static IServiceCollection AddDhcpServer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DhcpOptions>(configuration.GetSection("Dhcp"));
        return services.AddDhcpServices();
    }

    public static IServiceCollection AddDhcpServer(
        this IServiceCollection services,
        Action<DhcpOptions> configureOptions)
    {
        services.Configure(configureOptions);
        return services.AddDhcpServices();
    }

    private static IServiceCollection AddDhcpServices(this IServiceCollection services)
    {
        services.AddSingleton<ICidrPoolAllocator, CidrPoolAllocator>();
        services.AddSingleton<IArpConflictDetector, ArpConflictDetector>();
        services.AddSingleton<IDhcpLeaseEngine, DhcpLeaseEngine>();

        services.AddSingleton<DhcpServerEngine>();
        services.AddSingleton<IDhcpServerEngine>(sp => sp.GetRequiredService<DhcpServerEngine>());

        services.AddSingleton<IDhcpLeaseStore>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<DhcpOptions>>().Value;
            return new JsonDhcpLeaseStore(options.LeaseStorePath);
        });

        services.AddSingleton<IUdpTransport>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<DhcpOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<UdpTransport>>();
            return new UdpTransport(
                IPAddress.Parse(options.ListenAddress),
                options.ListenPort,
                logger);
        });

	services.AddSingleton<IDhcpRuntimeLoader, DhcpRuntimeLoader>();
        services.AddHostedService<DhcpHostedService>();

        return services;
    }
}
