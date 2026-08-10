using System.Net;

using Astrolabed.WebUI;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Astrolabed.Hosting;

public static class WebUiSidecar
{
    public static void StartIfEnabled(IHost mainHost, ServerOptions serverOptions, string[] args)
    {

        var logger = mainHost.Services.GetRequiredService<ILogger<Program>>();

        if (!serverOptions.WebUI.Enabled)
        {
            logger.LogWarning("WebUI not enabled.");
            return;
        }

        logger.LogInformation(
            "Starting WebUI sidecar on http://{Address}:{Port}",
            serverOptions.WebUI.ListenAddress,
            serverOptions.WebUI.ListenPort);

        var webUiHost = Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureWebHostDefaults(web =>
            {
                web.UseKestrel(options =>
                {
                    options.Listen(
                        IPAddress.Parse(serverOptions.WebUI.ListenAddress),
                        serverOptions.WebUI.ListenPort);

                    logger.LogInformation(
                        "Kestrel bound to {Address}:{Port}",
                        serverOptions.WebUI.ListenAddress,
                        serverOptions.WebUI.ListenPort);
                });

                web.Configure(app =>
                {

                    app.UseDefaultFiles();
                    app.UseStaticFiles();

                    app.UseRouting();

                    app.UseEndpoints(endpoints =>
                    {
                        WebUiRouting.RegisterRoutes(mainHost, endpoints);

		        endpoints.MapFallbackToFile("index.html");
                    });

                    logger.LogInformation("WebUI endpoints registered");
                });
            })
            .Build();

        _ = webUiHost.RunAsync();

        logger.LogInformation("WebUI sidecar started successfully.");
    }
}
