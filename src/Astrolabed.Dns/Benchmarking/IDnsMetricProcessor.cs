namespace Astrolabed.Dns.Benchmarking;

using System;
using System.Collections.Generic;

/// <summary>
/// Defines the contract for processing raw DNS endpoint metrics into ranked provider performance summaries.
/// </summary>
public interface IDnsMetricProcessor
{
    /// <summary>
    /// Processes structured DNS benchmark results and ranks DNS providers by average response speed.
    /// </summary>
    /// <param name="result">The benchmark result container containing endpoint metrics.</param>
    /// <returns>A read-only list of ranked DNS provider summaries sorted from fastest to slowest.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is <c>null</c>.</exception>
    IReadOnlyList<DnsServiceRanking> ProcessAndRank(DnsBenchmarkResult result);

}
