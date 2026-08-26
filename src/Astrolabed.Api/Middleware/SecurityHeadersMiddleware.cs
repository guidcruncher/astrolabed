namespace Astrolabed.Api.Middleware;

/// <summary>
/// Middleware to remove sensitive server footprint headers and inject security hardening headers.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityHeadersMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware delegate in the HTTP request pipeline.</param>
    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(next);
        _next = next;
    }

    /// <summary>
    /// Invokes the middleware to mutate response headers on completion.
    /// </summary>
    /// <param name="context">The current <see cref="HttpContext"/>.</param>
    /// <returns>A task representing asynchronous execution.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Response.OnStarting(() =>
        {
            IHeaderDictionary headers = context.Response.Headers;

            // 1. Strip signature headers that reveal implementation stack
            headers.Remove("X-Powered-By");
            headers.Remove("X-AspNet-Version");
            headers.Remove("X-AspNetMvc-Version");
            headers.Remove("Server");

            // 2. Prevent MIME-type sniffing
            headers.Append("X-Content-Type-Options", "nosniff");

            // 3. Prevent clickjacking / frame embedding
            headers.Append("X-Frame-Options", "DENY");

            // 4. Enforce HTTPS via HSTS (Strict-Transport-Security) for 1 year
            if (context.Request.IsHttps)
            {
                headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
            }

            // 5. Restrict referrer policy information leakage
            headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

            // 6. Basic Content-Security-Policy restricting script/frame execution
            // headers.Append("Content-Security-Policy", "default-src 'self'; frame-ancestors 'none'; object-src 'none';");

            return Task.CompletedTask;
        });

        await _next(context).ConfigureAwait(false);
    }
}
