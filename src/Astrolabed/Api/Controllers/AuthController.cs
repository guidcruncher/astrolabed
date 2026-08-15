using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

using Astrolabed.Api.Models;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Astrolabed.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;

    public AuthController(ILogger<AuthController> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user with username/password and sets an HTTP-only authentication cookie.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Username and password are required.");
        }

        // TODO: Replace this with your user repository validation/password hash checks
        if (!ValidateCredentials(request.Username, request.Password))
        {
            _logger.LogWarning("Failed login attempt for username '{Username}'", request.Username);
            return Unauthorized("Invalid credentials.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, request.Username),
            new(ClaimTypes.Name, request.Username),
            new(ClaimTypes.Role, "Admin")
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties).ConfigureAwait(false);

        _logger.LogInformation("User '{Username}' successfully logged in.", request.Username);

        return Ok(new UserDto(request.Username, true));
    }

    /// <summary>
    /// Logs out the current authenticated user by clearing the authentication cookie.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> LogoutAsync()
    {
        string username = User.Identity?.Name ?? "Unknown";

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);

        _logger.LogInformation("User '{Username}' logged out.", username);

        return Ok(new { Message = "Logged out successfully." });
    }

    /// <summary>
    /// Returns current authenticated user state.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public IActionResult GetCurrentUser()
    {
        string username = User.Identity?.Name ?? string.Empty;
        return Ok(new UserDto(username, true));
    }

    private static bool ValidateCredentials(string username, string password)
    {
        // Static validation stub - replace with secure password hashing verification (e.g. BCrypt, Argon2, Identity PasswordHasher)
        return username.Equals("admin", StringComparison.OrdinalIgnoreCase) && password == "Secret123!";
    }
}
