using System;
using System.ComponentModel.DataAnnotations;
using System.Net;

using Astrolabed.Data;
using Astrolabed.Data.Entities;
using Astrolabed.Data.Repositories;
using Astrolabed.Events;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Astrolabed.Api.Controllers;

[ApiController]
[Route("api/v1/dns-events")]
[Produces("application/json")]
[Authorize]
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

    /// <summary>
    /// Retrieves a paged list of all DNS response events.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<DnsResponseEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetAll(
        [FromQuery, Range(1, int.MaxValue)] int pageNumber = 1,
        [FromQuery, Range(1, 1000)] int pageSize = 100)
    {
        _logger.LogInformation("Retrieving paged DNS response events (Page: {PageNumber}, Size: {PageSize})", pageNumber, pageSize);
        var results = _repository.GetAll(pageNumber, pageSize);

        return Ok(results);
    }

    /// <summary>
    /// Retrieves a paged list of DNS response events within a specified time range.
    /// </summary>
    [HttpGet("range")]
    [ProducesResponseType(typeof(PagedResult<DnsResponseEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetByTimeRange(
        [FromQuery, Required] DateTimeOffset start,
        [FromQuery, Required] DateTimeOffset end,
        [FromQuery, Range(1, int.MaxValue)] int pageNumber = 1,
        [FromQuery, Range(1, 1000)] int pageSize = 100)
    {
        if (start >= end)
        {
            return BadRequest("Start timestamp must be strictly earlier than end timestamp.");
        }

        _logger.LogInformation("Retrieving DNS response events between {Start} and {End} (Page: {PageNumber}, Size: {PageSize})", start, end, pageNumber, pageSize);
        var results = _repository.GetByTimeRange(start, end, pageNumber, pageSize);

        return Ok(results);
    }

    /// <summary>
    /// Retrieves a paged list of DNS response events filtered by client IP address.
    /// </summary>
    [HttpGet("client/{clientIp}")]
    [ProducesResponseType(typeof(PagedResult<DnsResponseEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetByClientIp(
        string clientIp,
        [FromQuery, Range(1, int.MaxValue)] int pageNumber = 1,
        [FromQuery, Range(1, 1000)] int pageSize = 100)
    {
        if (!IPAddress.TryParse(clientIp, out var parsedIp))
        {
            return BadRequest($"Invalid IP address format: '{clientIp}'.");
        }

        _logger.LogInformation("Retrieving DNS response events for client IP {ClientIp} (Page: {PageNumber}, Size: {PageSize})", clientIp, pageNumber, pageSize);
        var results = _repository.GetByClientIp(parsedIp, pageNumber, pageSize);

        return Ok(results);
    }

    /// <summary>
    /// Retrieves a paged list of DNS response events filtered by DNS status code (e.g. NOERROR, NXDOMAIN).
    /// </summary>
    [HttpGet("status/{status}")]
    [ProducesResponseType(typeof(PagedResult<DnsResponseEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetByStatus(
        string status,
        [FromQuery, Range(1, int.MaxValue)] int pageNumber = 1,
        [FromQuery, Range(1, 1000)] int pageSize = 100)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return BadRequest("Status parameter cannot be null or whitespace.");
        }

        _logger.LogInformation("Retrieving DNS response events with status {Status} (Page: {PageNumber}, Size: {PageSize})", status, pageNumber, pageSize);
        var results = _repository.GetByStatus(status, pageNumber, pageSize);

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
