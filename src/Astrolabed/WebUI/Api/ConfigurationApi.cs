using Astrolabed.Configuration;
using Astrolabed.Hosting;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Astrolabed.WebUI.Api;

public static class ConfigurationApi
{
    public static void Register(IEndpointRouteBuilder app)
    {
        // POST /api/system/restart
        app.MapPost("/api/system/restart", ([FromServices] IApplicationRestartManager appRestartService) =>
        {
            appRestartService.RequestRestart(Startup.Arguments);
        });

        // GET /api/configuration
        app.MapGet("/api/configuration", ([FromServices] IOptionsMonitor<ServerOptions> options) =>
        {
            return TypedResults.Ok(options.CurrentValue);
        });

        // POST /api/configuration
        app.MapPost("/api/configuration", (
            [FromBody] ServerOptions options,
            [FromServices] ConfigurationWriter configWriter) =>
        {
            configWriter.Write(options);
            return TypedResults.Ok();
        });
    }
}
