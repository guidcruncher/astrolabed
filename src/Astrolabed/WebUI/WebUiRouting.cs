using Astrolabed.WebUI.Api;

using Microsoft.AspNetCore.Routing;

namespace Astrolabed.Hosting;

public static class WebUiRouting
{
    public static void RegisterRoutes(IEndpointRouteBuilder endpoints)
    {
        ConfigurationApi.Register(endpoints);
    }
}
