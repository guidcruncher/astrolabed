// File: DnsBenchmarkOptions.cs
namespace Astrolabed.Dns.Benchmarking.Options;
/// <summary>
/// Configuration options for governing the behavior of the DNS benchmark execution.
/// </summary>
public sealed class DnsBenchmarkOptions
{
    /// <summary>
    /// The canonical configuration section name used for binding.
    /// </summary>
    public const string SectionName = "DnsBenchmark";

    /// <summary>
    /// Gets or sets the fully qualified domain name to query during the benchmark.
    /// </summary>
    public string QueryDomain { get; set; } = "example.com";

    /// <summary>
    /// Gets or sets the collection of DNS servers to be evaluated.
    /// </summary>
    public List<DnsServerConfig> Servers { get; set; } = [];

    /// <summary>
    /// Gets or sets the number of warmup queries to execute before measuring latency.
    /// </summary>
    public int WarmupCount { get; set; } = 1;

    /// <summary>
    /// Gets or sets the number of recorded query iterations to perform per endpoint.
    /// </summary>
    public int Iterations { get; set; } = 5;

    /// <summary>
    /// Gets or sets the timeout in milliseconds for each individual UDP query.
    /// </summary>
    public int TimeoutMilliseconds { get; set; } = 2000;
}
