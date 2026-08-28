// File: DnsBenchmarkController.cs
namespace Astrolabed.Api.Controllers;

using System;
using System.Threading;
using System.Threading.Tasks;

using Astrolabed.Dns.Benchmarking;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Exposes API endpoints for initiating and retrieving DNS benchmark diagnostics.
/// </summary>
[ApiController]
[Route("api/benchmarks")]
[Produces("application/json")]
public class DnsBenchmarkController : ControllerBase
{
    private readonly IDnsBenchmarker _benchmarker;
    private readonly IDnsMetricProcessor _metricProcessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="DnsBenchmarkController"/> class.
    /// </summary>
    /// <param name="benchmarker">The injected DNS benchmarking service.</param>
    /// <param name="metricProcessor">The DNS metric processor service used to aggregate and rank metrics.</param>
    /// <exception cref="ArgumentNullException">Thrown if the benchmarker dependency is null.</exception>
    public DnsBenchmarkController(IDnsBenchmarker benchmarker, IDnsMetricProcessor metricProcessor)
    {
        ArgumentNullException.ThrowIfNull(benchmarker);
        ArgumentNullException.ThrowIfNull(metricProcessor);
        _benchmarker = benchmarker;
        _metricProcessor = metricProcessor;
    }

    /// <summary>
    /// Executes a DNS query benchmark against all configured public DNS providers.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for connection closures.</param>
    /// <returns>A detailed aggregate of latency and packet loss metrics across all providers.</returns>
    /// <response code="200">Returns the benchmark results successfully.</response>
    [HttpGet]
    [ProducesResponseType(typeof(DnsBenchmarkResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<DnsBenchmarkResult>> BenchmarkAllServersAsync(CancellationToken cancellationToken)
    {
        DnsBenchmarkResult result = await _benchmarker.BenchmarkAllAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Performs a Benchmark and returns a batch of structured DNS endpoint metrics and returns provider speed rankings.
    /// </summary>
    /// <returns>An <see cref="ActionResult{T}"/> containing the list of ranked DNS providers.</returns>
    /// <response code="200">Returns the ranked list of DNS service providers ordered from fastest to slowest.</response>
    /// <response code="500">If an unhandled exception occurs while processing the metrics.</response>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(IReadOnlyList<DnsServiceRanking>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<DnsServiceRanking>>> ProcessMetrics(CancellationToken cancellationToken)
    {
        DnsBenchmarkResult result = await _benchmarker.BenchmarkAllAsync(cancellationToken);
        try
        {
            var rankings = _metricProcessor.ProcessAndRank(result);
            return Ok(rankings);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Processing Error " + ex.Message,
                Detail = "An unexpected error occurred while processing DNS benchmark telemetry.",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Executes a DNS query benchmark against a specific configured public DNS provider by name.
    /// </summary>
    /// <param name="serverName">The friendly name of the DNS server to test (e.g., "Cloudflare").</param>
    /// <param name="cancellationToken">A token to monitor for connection closures.</param>
    /// <returns>A detailed aggregate of latency and packet loss metrics for the requested provider.</returns>
    /// <response code="200">Returns the benchmark results successfully.</response>
    /// <response code="404">If the specified server name does not exist in the application configuration.</response>
    [HttpGet("{serverName}")]
    [ProducesResponseType(typeof(DnsBenchmarkResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DnsBenchmarkResult>> BenchmarkSingleServerAsync(
        [FromRoute] string serverName,
        CancellationToken cancellationToken)
    {
        DnsBenchmarkResult? result = await _benchmarker.BenchmarkServerAsync(serverName, cancellationToken);

        if (result is null)
        {
            return NotFound(new { Message = $"Configured DNS server '{serverName}' was not found." });
        }

        return Ok(result);
    }
}
