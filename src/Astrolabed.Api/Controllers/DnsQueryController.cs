// File: src/Astrolabed.Api/Controllers/DnsQueryController.cs
using System.ComponentModel.DataAnnotations;
using System.Net;

using Astrolabed.Dns.Models;
using Astrolabed.Dns.Services;

using Microsoft.AspNetCore.Mvc;

namespace Astrolabed.Dns.Api.Controllers;

/// <summary>
/// API Controller providing DNS query resolution endpoints.
/// </summary>
/// <param name="dnsQueryProcessor">The DNS query processing service.</param>
/// <param name="logger">Structured logger instance.</param>
[ApiController]
[Route("api/dns")]
[Produces("application/json")]
public sealed partial class DnsQueryController(
    IDnsQueryProcessor dnsQueryProcessor,
    ILogger<DnsQueryController> logger) : ControllerBase
{
    private readonly IDnsQueryProcessor _dnsQueryProcessor = dnsQueryProcessor ?? throw new ArgumentNullException(nameof(dnsQueryProcessor));
    private readonly ILogger<DnsQueryController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Executes a DNS query for a target domain name and record type using the requesting client's IP.
    /// </summary>
    /// <param name="domain">The fully qualified domain name to resolve (e.g., example.com).</param>
    /// <param name="type">The target DNS record type (defaults to A record).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The resolved DNS wire message response.</returns>
    [HttpGet("query")]
    [ProducesResponseType(typeof(DnsWireMessage), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> QueryAsync(
        [FromQuery, Required] string domain,
        [FromQuery] DnsType type = DnsType.A,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Domain",
                Detail = "Domain name parameter cannot be empty.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        // Extract client IP address directly from HTTP request context
        IPAddress clientIp = ResolveRequestClientIpAddress();
        var clientEndpoint = new IPEndPoint(clientIp, HttpContext.Connection.RemotePort);

        LogExecutingDnsQuery(_logger, domain, type, clientEndpoint.Address.ToString());

        DnsWireMessage? response = await _dnsQueryProcessor.ProcessQueryAsync(
            domain,
            type,
            clientEndpoint,
            ct).ConfigureAwait(false);

        if (response is null)
        {
            LogDnsQueryFailed(_logger, domain, type);

            return StatusCode(StatusCodes.Status502BadGateway, new ProblemDetails
            {
                Title = "DNS Resolution Failed",
                Detail = $"Unable to process or obtain a DNS response for domain '{domain}'.",
                Status = StatusCodes.Status502BadGateway
            });
        }

        return Ok(response);
    }

    /// <summary>
    /// Resolves the actual requesting client IP address from request headers or TCP connection context.
    /// </summary>
    private IPAddress ResolveRequestClientIpAddress()
    {
        // 1. Check for standard proxy header X-Forwarded-For
        if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) &&
            !string.IsNullOrWhiteSpace(forwardedFor.ToString()))
        {
            string firstIp = forwardedFor.ToString().Split(',')[0].Trim();
            if (IPAddress.TryParse(firstIp, out IPAddress? parsedForwardedIp))
            {
                return parsedForwardedIp;
            }
        }

        // 2. Fall back to socket connection IP address
        if (HttpContext.Connection.RemoteIpAddress is { } remoteIp)
        {
            return remoteIp;
        }

        // 3. Fallback default for edge/in-memory test scenarios
        return IPAddress.Loopback;
    }

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Executing DNS query for domain '{Domain}' with type '{Type}' from client '{ClientIp}'")]
    private static partial void LogExecutingDnsQuery(ILogger logger, string domain, DnsType type, string clientIp);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Warning,
        Message = "DNS query execution returned null for domain '{Domain}' with type '{Type}'")]
    private static partial void LogDnsQueryFailed(ILogger logger, string domain, DnsType type);
}
