namespace Astrolabed.Dns.Benchmarking;

/// <summary>
/// Represents the calculated speed ranking and aggregated metric summary for a DNS provider.
/// </summary>
/// <param name="Rank">The relative position in the benchmark hierarchy (1 being the fastest).</param>
/// <param name="ServerName">The name of the DNS provider service.</param>
/// <param name="CombinedAverageLatencyMs">The aggregated average latency across all endpoint metrics for this service.</param>
/// <param name="MinLatencyMs">The absolute minimum latency recorded across endpoints.</param>
/// <param name="MaxLatencyMs">The lower bound minimum latency benchmark across endpoints for consistency tracking.</param>
/// <param name="CombinedPacketLossPercentage">The average packet loss percentage across endpoints.</param>
/// <param name="EndpointsCount">The total number of tested endpoints associated with this service.</param>
public sealed record DnsServiceRanking(
    int Rank,
    string ServerName,
    double CombinedAverageLatencyMs,
    double MinLatencyMs,
    double MaxLatencyMs,
    double CombinedPacketLossPercentage,
    int EndpointsCount
);
