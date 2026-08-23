using Astrolabed.Data.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Astrolabed.Data.Extensions;

/// <summary>
/// Extension methods for configuring database initialization services and executing database schema migrations.
/// </summary>
public static class DatabaseInitializerServiceCollectionExtensions
{
    /// <summary>
    /// Registers database initialization and schema provider infrastructure with the service collection.
    /// </summary>
    /// <param name="services">The target <see cref="IServiceCollection"/> to register dependencies into.</param>
    /// <returns>The modified <see cref="IServiceCollection"/> for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddDatabaseInitializer(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ISchemaProvider, EmbeddedSchemaProvider>();
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();

        return services;
    }

    /// <summary>
    /// Asynchronously initializes and migrates the database schema using the configured <see cref="IDatabaseInitializer"/>.
    /// Should be invoked during application startup after building the <see cref="IHost"/>.
    /// </summary>
    /// <param name="host">The built application <see cref="IHost"/> instance.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests during initialization.</param>
    /// <returns>A task representing the asynchronous initialization operation, returning the original <see cref="IHost"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="host"/> is null.</exception>
    public static async Task<IHost> InitializeDatabaseAsync(
        this IHost host,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(host);

        await using AsyncServiceScope scope = host.Services.CreateAsyncScope();
        IDatabaseInitializer initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();

        await initializer.InitializeAsync(cancellationToken);

        return host;
    }
}
