using Astrolabed;
using Astrolabed.Api.Services;

using Microsoft.AspNetCore.Mvc;

namespace Astrolabed.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ConfigurationController : ControllerBase
{
    private readonly IAppConfigurationService _configService;
    private readonly ILogger<ConfigurationController> _logger;

    public ConfigurationController(
        IAppConfigurationService configService,
        ILogger<ConfigurationController> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ServerOptions), StatusCodes.Status200OK)]
    public ActionResult<ServerOptions> GetConfiguration()
    {
        _logger.LogInformation("Fetching current server options configuration");
        var config = _configService.GetConfiguration();
        return Ok(config);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateConfiguration(
        [FromBody] ServerOptions newConfig,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Updating server options configuration");
        await _configService.UpdateConfigurationAsync(newConfig, cancellationToken);

        return NoContent();
    }
}
