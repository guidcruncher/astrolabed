using Astrolabed.Configuration;
using Astrolabed.WebUI.Api;
using Astrolabed.Hosting;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Astrolabed.WebUI.Api;

public static class ConfigurationApi
{

    public static void Register(IHost mainHost, IEndpointRouteBuilder app)
    {
        var serverOptions = mainHost.Services.GetRequiredService<ServerOptions>();
        var appRestartService = mainHost.Services.GetRequiredService<IApplicationRestartManager>();

        // GET /api/system/restart
        app.MapGet("/api/system/restart", () =>
            {
                appRestartService.RequestRestart(Startup.arguments);
            });

        // GET /api/configuration
        app.MapGet("/api/configuration", () =>
        {
            return serverOptions;
        });

        app.MapPut("/api/configuration", ([FromBody] ServerOptions options) =>
            {
                var configWriter = new ConfigurationWriter();
                return configWriter.Write(options);
            });
    }
}
