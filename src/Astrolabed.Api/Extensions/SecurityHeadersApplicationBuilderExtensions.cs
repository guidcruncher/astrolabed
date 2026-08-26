using Astrolabed.Api.Middleware;

namespace Astrolabed.Api.Extensions;

/// <summary>
/// Extension methods for configuring security headers in an <see cref="IApplicationBuilder"/>.
/// </summary>
public static class SecurityHeadersApplicationBuilderExtensions
{
    /// <summary>
    /// Adds custom security hardening and header-stripping middleware to the HTTP request pipeline.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> instance.</param>
    /// <returns>The updated <see cref="IApplicationBuilder"/> for chaining.</returns>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
