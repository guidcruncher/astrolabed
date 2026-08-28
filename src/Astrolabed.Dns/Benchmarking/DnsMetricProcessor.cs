namespace Astrolabed.Dns.Benchmarking;

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;

/// <summary>
/// Processes DNS endpoint telemetry and aggregates metrics to rank providers by speed and performance.
/// Target implementation for .NET 10.
/// </summary>
public sealed class DnsMetricProcessor : IDnsMetricProcessor
{
    private readonly ILogger<DnsMetricProcessor> _logger;
    private const int _decimalPlaces = 3;

    /// <summary>
    /// Initializes a new instance of the <see cref="DnsMetricProcessor"/> class.
    /// </summary>
    /// <param name="logger">The logging provider instance for structured telemetry recording.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="logger"/> is <c>null</c>.
    /// </exception>
    public DnsMetricProcessor(
        ILogger<DnsMetricProcessor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes structured DNS benchmark results and ranks DNS providers by average response speed.
    /// </summary>
    /// <param name="result">The benchmark result container containing endpoint metrics.</param>
    /// <returns>A read-only list of ranked DNS provider summaries sorted from fastest to slowest.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is <c>null</c>.</exception>
    public IReadOnlyList<DnsServiceRanking> ProcessAndRank(DnsBenchmarkResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.EndpointMetrics is null || result.EndpointMetrics.Count == 0)
        {
            _logger.LogWarning("Received empty or null endpoint metrics dataset.");
            return [];
        }

        _logger.LogInformation("Processing latency metrics for {Count} DNS endpoints executed at {ExecutedAt}.",
            result.EndpointMetrics.Count,
            result.ExecutedAt);

        var groupedMetrics = result.EndpointMetrics
            .GroupBy(e => e.ServerName, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                double combinedAvgLatency = group.Average(e => e.AverageLatencyMs);
                double minLatency = group.Min(e => e.MinimumLatencyMs);
                double maxLatency = group.Min(e => e.MinimumLatencyMs);
                double combinedPacketLoss = group.Average(e => e.PacketLossPercentage);

                return new
                {
                    ServerName = group.Key,
                    CombinedAverageLatencyMs = Math.Round(combinedAvgLatency, _decimalPlaces),
                    MinLatencyMs = Math.Round(minLatency, _decimalPlaces),
                    MaxLatencyMs = Math.Round(maxLatency, _decimalPlaces),
                    CombinedPacketLossPercentage = Math.Round(combinedPacketLoss, _decimalPlaces),
                    EndpointsCount = group.Count()
                };
            })
            .OrderBy(m => m.CombinedAverageLatencyMs)
            .ThenBy(m => m.CombinedPacketLossPercentage)
            .Select((item, index) => new DnsServiceRanking(
                Rank: index + 1,
                ServerName: item.ServerName,
                CombinedAverageLatencyMs: item.CombinedAverageLatencyMs,
                MinLatencyMs: item.MinLatencyMs,
                MaxLatencyMs: item.MaxLatencyMs,
                CombinedPacketLossPercentage: item.CombinedPacketLossPercentage,
                EndpointsCount: item.EndpointsCount
            ))
            .ToList();

        _logger.LogInformation("Successfully ranked {Count} unique DNS providers.", groupedMetrics.Count);

        return groupedMetrics.AsReadOnly();
    }

}
