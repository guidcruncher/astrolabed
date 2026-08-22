using Astrolabed.Ntp.Options;
using Astrolabed.Ntp.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Astrolabed.Ntp.Extensions;

public static class NtpServiceCollectionExtensions
{
    public static IServiceCollection AddNtpServer(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<NtpServerOptions>(configuration.GetSection(NtpServerOptions.SectionName));

        services.AddSingleton<LocalTimeResolver>();
        services.AddSingleton<UpstreamTimeResolver>();
        services.AddSingleton<ITimeResolver, CompositeTimeResolver>();

        services.AddSingleton<INtpServerHandler, NtpServerHandler>();
        services.AddHostedService<NtpServerEngine>();

        return services;
    }

    public static IServiceCollection AddNtpServer(this IServiceCollection services, Action<NtpServerOptions> configureOptions)
    {
        services.Configure(configureOptions);

        services.AddSingleton<LocalTimeResolver>();
        services.AddSingleton<UpstreamTimeResolver>();
        services.AddSingleton<ITimeResolver, CompositeTimeResolver>();

        services.AddSingleton<INtpServerHandler, NtpServerHandler>();
        services.AddHostedService<NtpServerEngine>();

        return services;
    }
}
