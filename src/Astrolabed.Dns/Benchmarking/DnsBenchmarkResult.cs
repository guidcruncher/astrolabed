// File: DnsBenchmarkResult.cs
namespace Astrolabed.Dns.Benchmarking;

using System;
using System.Collections.Generic;

/// <summary>
/// Represents the aggregate results of a complete DNS benchmark operation.
/// </summary>
/// <param name="EndpointMetrics">The collection of metrics gathered from each evaluated endpoint.</param>
/// <param name="ExecutedAt">The timestamp indicating when the benchmark completed.</param>
public sealed record DnsBenchmarkResult(
    IReadOnlyList<DnsEndpointMetrics> EndpointMetrics,
    DateTimeOffset ExecutedAt
);
