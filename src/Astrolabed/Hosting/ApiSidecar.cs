using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

using Astrolabed.Api;
using Astrolabed.Api.Controllers;
using Astrolabed.Configuration;
using Astrolabed.Dhcp;
using Astrolabed.Dns.RuleEngine;
using Astrolabed.Serialization;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
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

                // Configure Cookie Authentication
                services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(options =>
                    {
                        options.Cookie.Name = "Astrolabed.Auth";
                        options.Cookie.HttpOnly = true;
                        options.Cookie.SameSite = SameSiteMode.Strict;
                        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

                        options.Events.OnRedirectToLogin = context =>
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            return Task.CompletedTask;
                        };
                        options.Events.OnRedirectToAccessDenied = context =>
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            return Task.CompletedTask;
                        };
                    });

                services.AddAuthorization();

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
                web.UseSetting(WebHostDefaults.EnvironmentKey,
                    mainHost.Services.GetRequiredService<IHostEnvironment>().EnvironmentName);

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

                    var contentPath = "/app/ClientUI";

                    if (context.HostingEnvironment.IsDevelopment())
                    {
                        contentPath = Path.GetFullPath(
                            Path.Combine(context.HostingEnvironment.ContentRootPath, "../ClientUI/dist/"));
                    }

                    logger.LogInformation($"Kestrel is serving Client from {contentPath}");
                    web.UseWebRoot(contentPath);

                    var fileProvider = new PhysicalFileProvider(contentPath);

                    app.UseDefaultFiles(new DefaultFilesOptions
                    {
                        FileProvider = fileProvider,
                        RequestPath = ""
                    });

                    app.UseStaticFiles(new StaticFileOptions
                    {
                        FileProvider = fileProvider,
                        RequestPath = ""
                    });

                    app.UseAuthentication();
                    app.UseAuthorization();

                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllers();

                        // Vue SPA fallback routing
                        endpoints.MapFallbackToFile("index.html");

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
