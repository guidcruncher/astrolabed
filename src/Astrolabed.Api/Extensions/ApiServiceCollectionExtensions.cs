using Astrolabed.Api.Options;
using Astrolabed.Api.Services;

namespace Astrolabed.Api.Extensions;

/// <summary>
/// Extension methods for setting up Astrolabed API services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class ApiServiceCollectionExtensions
{
    /// <summary>
    /// Registers Astrolabed API services, controllers, and options with the DI container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The application <see cref="IConfiguration"/> instance.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<ApiOptions>(
            configuration.GetSection(ApiOptions.SectionName));

        services.AddScoped<IAstrolabedService, AstrolabedService>();

        services.AddControllers()
                .AddApplicationPart(typeof(ApiServiceCollectionExtensions).Assembly);

        return services;
    }
}
