using Astrolabed.WebUI.Api;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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

        // GET /api/configuration
        app.MapGet("api/configuration", () =>
        {
            return serverOptions;
        });

    }
}
