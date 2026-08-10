using Astrolabed.WebUI.Api;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Astrolabed.Hosting;

public static class WebUiRouting
{

    public static void RegisterRoutes(IHost mainHost, IEndpointRouteBuilder endpoints)
    {
        ConfigurationApi.Register(mainHost, endpoints);
    }

}
