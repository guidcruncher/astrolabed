using Astrolabed.Data.Repositories;
using Astrolabed.Dhcp.Options;
using Astrolabed.Dhcp.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Astrolabed.Dhcp.Extensions;

public static class DhcpServerServiceCollectionExtensions
{
    public static IServiceCollection AddDhcpServer(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DhcpServerOptions>(configuration.GetSection(DhcpServerOptions.Position));

        services.AddSingleton<IDhcpLeaseRepository, InMemoryDhcpLeaseRepository>();
        services.AddScoped<IDhcpHandler, DhcpHandler>();
        services.AddHostedService<DhcpEngine>();

        return services;
    }
}
