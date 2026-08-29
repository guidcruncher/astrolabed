using Astrolabed.Core.Network;

using Microsoft.Extensions.DependencyInjection;

namespace Astrolabed.Core.Extensions;

/// <summary>
/// Extension methods for registering MAC vendor lookup services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class MacVendorLookupServiceCollectionExtensions
{
    /// <summary>
    /// Adds the <see cref="IMacVendorLookupService"/> singleton service to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The original <see cref="IServiceCollection"/> for chaining calls.</returns>
    public static IServiceCollection AddMacVendorLookup(this IServiceCollection services)
    {
        services.AddSingleton<IMacVendorLookupService, MacVendorLookupService>();
        return services;
    }
}
