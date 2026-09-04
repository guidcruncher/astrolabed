using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Astrolabed.Core.Options;
using Astrolabed.Core.Network;

namespace Astrolabed.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMetworkServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<PingServiceOptions>(
            configuration.GetSection(PingServiceOptions.SectionName));

        services.AddTransient<IPingService, PingService>();

        return services;
    }
}
