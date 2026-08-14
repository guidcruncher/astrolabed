using System.Net;

using Astrolabed.Api.Services;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Astrolabed.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class TimeController : ControllerBase

{
    private readonly INtpService _ntpService;
    private readonly ILogger<TimeController> _logger;

    public TimeController(INtpService ntpService, ILogger<TimeController> logger)
    {
        _ntpService = ntpService ?? throw new ArgumentNullException(nameof(ntpService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Returns current synchronized time and raw NTP packet response data using Astrolabed NTP core.
    /// </summary>
    [HttpGet("ntp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetNtpTime(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing NTP time request via internal Astrolabed NTP service.");

        try
        {
            var response = await _ntpService.QueryAsync(cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve time from internal Astrolabed NTP service.");

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "NTP Service Error",
                Detail = "The internal Astrolabed NTP service was unable to fulfill the request.",
                Instance = HttpContext.Request.Path
            });
        }
    }
}
