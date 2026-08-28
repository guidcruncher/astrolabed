// File: DnsEndpointMetrics.cs
namespace Astrolabed.Dns.Benchmarking;

using System.Net.Sockets;

/// <summary>
/// Represents the aggregated performance and availability metrics for a single DNS endpoint.
/// </summary>
/// <param name="ServerName">The name of the DNS provider.</param>
/// <param name="IpAddress">The specific IP address that was queried.</param>
/// <param name="AddressFamily">The network address family (IPv4 or IPv6) of the endpoint.</param>
/// <param name="IsSuccessful">Indicates whether any queries successfully returned a response.</param>
/// <param name="MinimumLatencyMs">The minimum recorded latency in milliseconds.</param>
/// <param name="AverageLatencyMs">The average recorded latency in milliseconds.</param>
/// <param name="MaximumLatencyMs">The maximum recorded latency in milliseconds.</param>
/// <param name="PacketLossPercentage">The percentage of queries that timed out or failed.</param>
/// <param name="ErrorMessage">The error message from the last failed attempt, if applicable.</param>
public sealed record DnsEndpointMetrics(
    string ServerName,
    string IpAddress,
    AddressFamily AddressFamily,
    bool IsSuccessful,
    double MinimumLatencyMs,
    double AverageLatencyMs,
    double MaximumLatencyMs,
    double PacketLossPercentage,
    string? ErrorMessage
);
