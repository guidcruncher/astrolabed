using Astrolabed.Api.Options;
using Astrolabed.Data.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Astrolabed.Api.Controllers;

/// <summary>
/// Provides authentication endpoints for managing user sessions, including login, logout, and current user retrieval.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AuthController> _logger;
    private readonly AuthOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    /// <param name="signInManager">The sign-in manager for handling authentication operations.</param>
    /// <param name="userManager">The user manager for managing user accounts.</param>
    /// <param name="logger">The logger instance for auth controller logging.</param>
    /// <param name="options">The authentication options configured for the application.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required dependency is null.</exception>
    public AuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<AuthController> logger,
        IOptions<AuthOptions> options)
    {
        _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Authenticates a user with email and password credentials.
    /// </summary>
    /// <param name="request">The login request payload containing email, password, and session preferences.</param>
    /// <returns>An <see cref="IActionResult"/> indicating the result of the authentication attempt.</returns>
    /// <response code="200">Returns user information upon successful authentication.</response>
    /// <response code="400">If the request payload fails model validation.</response>
    /// <response code="401">If credentials are invalid or the user does not exist.</response>
    /// <response code="423">If the user account is locked out.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            _logger.LogWarning("Failed login attempt for non-existent email: {Email}", request.Email);
            return Unauthorized(new { Message = "Invalid credentials." });
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!,
            request.Password,
            request.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("User logged in successfully: {UserId}", user.Id);
            return Ok(new UserDto(user.Id, user.Email!, user.DisplayName));
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User account locked out: {UserId}", user.Id);
            return StatusCode(StatusCodes.Status423Locked, new { Message = "Account is locked." });
        }

        _logger.LogWarning("Invalid password attempt for user: {UserId}", user.Id);
        return Unauthorized(new { Message = "Invalid credentials." });
    }

    /// <summary>
    /// Signs out the currently authenticated user and terminates their session.
    /// </summary>
    /// <returns>An <see cref="IActionResult"/> indicating successful logout.</returns>
    /// <response code="200">User successfully logged out.</response>
    /// <response code="401">If the requesting user is unauthenticated.</response>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out successfully.");
        return Ok(true);
    }

    /// <summary>
    /// Retrieves details for the currently authenticated user session.
    /// </summary>
    /// <returns>User details for the current session.</returns>
    /// <response code="200">Returns profile details for the authenticated user.</response>
    /// <response code="401">If the user is not authenticated.</response>
    /// <response code="404">If the user identity cannot be located in storage.</response>
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(new UserDto(user.Id, user.Email!, user.DisplayName));
    }
}

/// <summary>
/// Represents the payload for user authentication requests.
/// </summary>
/// <param name="Email">The user's registered email address.</param>
/// <param name="Password">The plain-text user password for authentication.</param>
/// <param name="RememberMe">Indicates whether to persist the authentication session across browser restarts.</param>
public record LoginRequest(string Email, string Password, bool RememberMe = false);

/// <summary>
/// Data transfer object representing public user details.
/// </summary>
/// <param name="Id">The unique user identifier.</param>
/// <param name="Email">The user's primary email address.</param>
/// <param name="DisplayName">The user's optional display name.</param>
public record UserDto(string Id, string Email, string? DisplayName);
