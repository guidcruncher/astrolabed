using Astrolabed.Api.Data;
using Astrolabed.Api.Options;

using Microsoft.AspNetCore.Identity;

namespace Astrolabed.Api.Extensions;

/// <summary>
/// Provides extension methods for registering and configuring authentication services in the dependency injection container.
/// </summary>
public static class AuthenticationServiceCollectionExtensions
{
    /// <summary>
    /// Adds application authentication, ASP.NET Core Identity, and cookie security configuration to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to add authentication services to.</param>
    /// <param name="configuration">The configuration instance containing authentication settings.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> instance to enable fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configuration"/> is null.</exception>
    public static IServiceCollection AddAppAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Bind options via Microsoft.Extensions.Options
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));

        var authOptions = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>()
                          ?? new AuthOptions();

        // Configure ASP.NET Core Identity
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.User.RequireUniqueEmail = true;
        })
        .AddDefaultTokenProviders();

        // Configure HttpOnly Cookie Authentication
        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = authOptions.CookieName;
            options.Cookie.HttpOnly = true; // Prevents XSS token access
            options.Cookie.SameSite = SameSiteMode.Strict; // Prevents CSRF
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Requires HTTPS
            options.ExpireTimeSpan = TimeSpan.FromDays(authOptions.ExpireDays);
            options.SlidingExpiration = authOptions.SlidingExpiration;

            // Prevent 302 redirects for SPA API endpoints in .NET 10
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

        return services;
    }
}
