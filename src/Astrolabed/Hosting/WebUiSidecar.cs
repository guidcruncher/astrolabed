using System.Net;

using Astrolabed.Configuration;
using Astrolabed.WebUI;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.Hosting;

public static class WebUiSidecar
{
    public static void StartIfEnabled(IHost mainHost, ServerOptions serverOptions, string[] args)
    {
        var logger = mainHost.Services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(WebUiSidecar));

        if (!serverOptions.WebUI.Enabled)
        {
            logger.LogWarning("WebUI not enabled.");
            return;
        }

        logger.LogInformation(
            "Starting WebUI sidecar on http://{Address}:{Port}",
            serverOptions.WebUI.ListenAddress,
            serverOptions.WebUI.ListenPort);

        var lifetime = mainHost.Services.GetRequiredService<IHostApplicationLifetime>();
        var mainConfig = mainHost.Services.GetRequiredService<IConfiguration>();

        var webUiHost = Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton(mainConfig);
                services.AddSingleton(mainHost.Services.GetRequiredService<IApplicationRestartManager>());
                services.AddTransient<ConfigurationWriter>();
                services.Configure<ServerOptions>(mainConfig);
            })
            .ConfigureWebHostDefaults(web =>
            {
                web.UseKestrel(options =>
                {
                    if (IPAddress.TryParse(serverOptions.WebUI.ListenAddress, out var ip))
                    {
                        options.Listen(ip, serverOptions.WebUI.ListenPort);
                    }
                    else
                    {
                        options.ListenAnyIP(serverOptions.WebUI.ListenPort);
                    }

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
                        WebUiRouting.RegisterRoutes(endpoints);

                        endpoints.MapFallbackToFile("index.html");
                    });

                    logger.LogInformation("WebUI endpoints registered");
                });
            })
            .Build();

        _ = webUiHost.RunAsync(lifetime.ApplicationStopping).ContinueWith(t =>
        {
            if (t.IsFaulted && t.Exception is not null)
            {
                logger.LogError(t.Exception, "WebUI sidecar encountered an error during execution.");
            }
        });

        logger.LogInformation("WebUI sidecar started successfully.");
    }
}
