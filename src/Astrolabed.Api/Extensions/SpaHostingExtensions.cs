using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Astrolabed.Api.Extensions;

public static class SpaHostingExtensions
{
    private const string DefaultFallbackFile = "index.html";

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
