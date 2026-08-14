using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json.Nodes;

using Astrolabed.Api;
using Astrolabed.Api.Controllers;
using Astrolabed.Configuration;
using Astrolabed.Dhcp;
using Astrolabed.Dns.RuleEngine;
using Astrolabed.Serialization;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

using Scalar.AspNetCore;

namespace Astrolabed.Hosting;

public static class ApiSidecar
{
    public static void StartIfEnabled(IHost mainHost, ServerOptions serverOptions, string[] args, IDnsCache sharedCache)
    {
        var logger = mainHost.Services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(ApiSidecar));

        if (!serverOptions.WebUI.Enabled)
        {
            logger.LogWarning("API sidecar not enabled.");
            return;
        }

        logger.LogInformation(
            "Starting API sidecar on http://{Address}:{Port}",
            serverOptions.WebUI.ListenAddress,
            serverOptions.WebUI.ListenPort);

        logger.LogInformation($"System Shared DNS Cache Instance Identifier {sharedCache.InstanceId}");

        var lifetime = mainHost.Services.GetRequiredService<IHostApplicationLifetime>();
        var mainConfig = mainHost.Services.GetRequiredService<IConfiguration>();

        var apiHost = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(config =>
            {
                config.AddConfiguration(mainConfig);
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConfiguration(mainConfig.GetSection("Logging"));
                logging.AddConsole();
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton(mainConfig);
                services.Configure<ServerOptions>(mainConfig);

                services.AddApiServices(mainHost, mainConfig, sharedCache);

                services.ConfigureHttpJsonOptions(options =>
                {
                    options.SerializerOptions.Converters.Add(new IPAddressJsonConverter());
                    options.SerializerOptions.Converters.Add(new PhysicalAddressJsonConverter());
                });

                services.AddControllers()
                    .AddControllersFromNamespace<LeasesController>("Astrolabed.Api.Controllers")
                    .AddJsonOptions(options =>
                    {
                        options.JsonSerializerOptions.Converters.Add(new IPAddressJsonConverter());
                        options.JsonSerializerOptions.Converters.Add(new PhysicalAddressJsonConverter());
                    });

                services.AddOpenApi(options =>
                {
                    options.AddSchemaTransformer((schema, context, cancellationToken) =>
                    {
                        if (context.JsonTypeInfo.Type == typeof(IPAddress))
                        {
                            schema.Type = JsonSchemaType.String;
                            schema.Example = JsonValue.Create("192.168.1.50");
                        }
                        else if (context.JsonTypeInfo.Type == typeof(PhysicalAddress))
                        {
                            schema.Type = JsonSchemaType.String;
                            schema.Example = JsonValue.Create("00:11:22:33:44:55");
                        }

                        return Task.CompletedTask;
                    });
                });
            })
            .ConfigureWebHostDefaults(web =>
            {
                web.UseKestrel(options =>
                {
		    options.AddServerHeader = false;
                    if (IPAddress.TryParse(serverOptions.WebUI.ListenAddress, out var ip))
                    {
                        options.Listen(ip, serverOptions.WebUI.ListenPort);
                    }
                    else
                    {
                        options.ListenAnyIP(serverOptions.WebUI.ListenPort);
                    }

                    logger.LogInformation(
                        "Kestrel bound API sidecar to {Address}:{Port}",
                        serverOptions.WebUI.ListenAddress,
                        serverOptions.WebUI.ListenPort);
                });

                web.Configure((context, app) =>
                {
                    app.UseRouting();

                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllers();

                        if (context.HostingEnvironment.IsDevelopment())
                        {
                            endpoints.MapOpenApi();
                            endpoints.MapScalarApiReference("/docs/");
                            logger.LogInformation("OpenApi Documentation enabled at /openapi/v1.json and /scalar");
                        }
                        else
                        {
                            logger.LogWarning("OpenApi Documentation disabled");
                        }
                    });

                    logger.LogInformation("API sidecar controllers registered successfully.");
                });
            })
            .Build();

        _ = apiHost.RunAsync(lifetime.ApplicationStopping).ContinueWith(t =>
        {
            if (t.IsFaulted && t.Exception is not null)
            {
                logger.LogError(t.Exception, "API sidecar encountered an error during execution.");
            }
        });

        logger.LogInformation("API sidecar started successfully.");
    }
}

