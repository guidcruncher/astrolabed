using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Astrolabed.Api.Extensions;

/// <summary>
/// Provides extension methods for configuring Single Page Application (SPA) static file hosting and fallback routing.
/// </summary>
public static class SpaHostingExtensions
{
    private const string DefaultFallbackFile = "index.html";

    /// <summary>
    /// Configures the request pipeline to serve static assets for a Single Page Application (SPA)
    /// and maps unhandled routes to the default fallback file.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> instance being configured.</param>
    /// <returns>The updated <see cref="WebApplication"/> instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="app"/> is <see langword="null"/>.</exception>
    public static WebApplication UseSpaHosting(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        ILoggerFactory loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
        ILogger logger = loggerFactory.CreateLogger(typeof(SpaHostingExtensions));

        logger.LogInformation("Enabling default file middleware for SPA.");
        app.UseDefaultFiles();

        logger.LogInformation("Enabling static file middleware for SPA.");
        app.UseStaticFiles();

        logger.LogInformation("Mapping SPA fallback route to '{FallbackFile}'.", DefaultFallbackFile);
        app.MapFallbackToFile(DefaultFallbackFile);

        return app;
    }
}
