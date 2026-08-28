// File: IDnsBenchmarker.cs
namespace Astrolabed.Dns.Benchmarking;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Defines the contract for executing DNS performance and availability benchmarks.
/// </summary>
public interface IDnsBenchmarker
{
    /// <summary>
    /// Executes a benchmark against all configured DNS servers.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation, containing the overall benchmark results.</returns>
    Task<DnsBenchmarkResult> BenchmarkAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a benchmark against a specific configured DNS server by name.
    /// </summary>
    /// <param name="serverName">The name of the server to benchmark (case-insensitive).</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task containing the benchmark results, or null if the server name is not found in the configuration.</returns>
    Task<DnsBenchmarkResult?> BenchmarkServerAsync(string serverName, CancellationToken cancellationToken = default);
}
