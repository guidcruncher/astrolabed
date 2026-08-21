using Astrolabed.Data;
using Astrolabed.Data.Options;
using Astrolabed.Data.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Astrolabed.Data.Extensions;

/// <summary>
/// Service collection extensions for registering the database initializer infrastructure.
/// </summary>
public static class DatabaseInitializerServiceCollectionExtensions
{
    public static IServiceCollection AddDatabaseInitializer(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTransient<ISchemaProvider, EmbeddedSchemaProvider>();
        services.AddTransient<IDatabaseInitializer, DatabaseInitializer>();

        return services;
    }

    public static async Task<IServiceCollection> InitializeDatabase(this IServiceCollection services)
    {

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            IDatabaseInitializer initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
            await initializer.InitializeAsync();
        }

        return services;
    }

}
