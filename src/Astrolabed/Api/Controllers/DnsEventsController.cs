using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Data.Entities;
using Astrolabed.Data.Repositories;
using Astrolabed.Events;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Astrolabed.Api.Controllers;

[ApiController]
[Route("api/v1/dns-events")]
[Produces("application/json")]
public sealed class DnsEventsController : ControllerBase
{
    private readonly IDnsResponseEventRepository _repository;
    private readonly ILogger<DnsEventsController> _logger;

    public DnsEventsController(
        IDnsResponseEventRepository repository,
        ILogger<DnsEventsController> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(logger);

        _repository = repository;
        _logger = logger;
    }

    [HttpGet("/")]
    [ProducesResponseType(typeof(IEnumerable<DnsResponseEvent>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetAll(
            string status,
            [FromQuery, Range(1, 1000)] int limit = 100)
    {
        _logger.LogInformation("Retrieving All DNS response events limit {Limit}", limit);
        var results = _repository.GetAll(limit);

        return Ok(results);
    }

    [HttpGet("range")]
    [ProducesResponseType(typeof(IEnumerable<DnsResponseEvent>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetByTimeRange(
        [FromQuery, Required] DateTimeOffset start,
        [FromQuery, Required] DateTimeOffset end,
        [FromQuery, Range(1, 1000)] int limit = 100)
    {
        if (start >= end)
        {
            return BadRequest("Start timestamp must be strictly earlier than end timestamp.");
        }

        _logger.LogInformation("Retrieving DNS response events between {Start} and {End} (Limit: {Limit})", start, end, limit);
        var results = _repository.GetByTimeRange(start, end, limit);

        return Ok(results);
    }

    /// <summary>
    /// Retrieves DNS response events filtered by client IP address.
    /// </summary>
    [HttpGet("client/{clientIp}")]
    [ProducesResponseType(typeof(IEnumerable<DnsResponseEvent>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetByClientIp(
        string clientIp,
        [FromQuery, Range(1, 1000)] int limit = 100)
    {
        if (!IPAddress.TryParse(clientIp, out var parsedIp))
        {
            return BadRequest($"Invalid IP address format: '{clientIp}'.");
        }

        _logger.LogInformation("Retrieving DNS response events for client IP {ClientIp} (Limit: {Limit})", clientIp, limit);
        var results = _repository.GetByClientIp(parsedIp, limit);

        return Ok(results);
    }

    /// <summary>
    /// Retrieves DNS response events filtered by DNS status code (e.g. NOERROR, NXDOMAIN).
    /// </summary>
    [HttpGet("status/{status}")]
    [ProducesResponseType(typeof(IEnumerable<DnsResponseEvent>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetByStatus(
        string status,
        [FromQuery, Range(1, 1000)] int limit = 100)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return BadRequest("Status parameter cannot be null or whitespace.");
        }

        _logger.LogInformation("Retrieving DNS response events with status {Status} (Limit: {Limit})", status, limit);
        var results = _repository.GetByStatus(status, limit);

        return Ok(results);
    }

    /// <summary>
    /// Deletes DNS response events older than a given cutoff date.
    /// </summary>
    [HttpDelete("purge")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult DeleteOlderThan([FromQuery, Required] DateTimeOffset cutoff)
    {
        _logger.LogWarning("Purging DNS response events older than {Cutoff}", cutoff);
        int deletedCount = _repository.DeleteOlderThan(cutoff);

        return Ok(new { Cutoff = cutoff, DeletedCount = deletedCount });
    }
}
