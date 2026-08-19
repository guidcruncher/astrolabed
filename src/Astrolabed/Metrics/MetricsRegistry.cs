using System.Text;

using Astrolabed.Dns.Core;
using Astrolabed.Events;

namespace Astrolabed.Metrics;

public sealed class MetricsRegistry
{
    private long _dnsQueriesTotal;
    private long _dnsResponsesTotal;
    private long _dnsNxDomainTotal;
    private long _dnsServFailTotal;

    private long _dnsCacheHitsTotal;

    private readonly double[] _dnsLatencyBuckets =
        new double[] { 0.001, 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1, 2 };

    private readonly long[] _dnsLatencyCounts;
    private double _dnsLatencySum;
    private long _dnsLatencyTotalCount;

    private long _dhcpLeaseAllocationsTotal;
    private long _dhcpLeasesActive;

    private long _ntpSyncTotal;
    private long _ntpSyncFailuresTotal;
    private double _ntpOffsetMs;

    public MetricsRegistry()
    {
        _dnsLatencyCounts = new long[_dnsLatencyBuckets.Length];
    }

    // -----------------------------
    // DNS Metrics
    // -----------------------------

    public void RecordDnsQuery(DnsQueryEvent evt)
    {
        Interlocked.Increment(ref _dnsQueriesTotal);
    }

    public void RecordDnsResponse(DnsResponseEvent evt)
    {
        Interlocked.Increment(ref _dnsResponsesTotal);

        if (string.Equals(evt.Status.ToMnemonic(), "NXDOMAIN", StringComparison.OrdinalIgnoreCase))
        {
            Interlocked.Increment(ref _dnsNxDomainTotal);
        }
        else if (string.Equals(evt.Status.ToMnemonic(), "SERVFAIL", StringComparison.OrdinalIgnoreCase))
        {
            Interlocked.Increment(ref _dnsServFailTotal);
        }
    }

    public void RecordDnsCacheHit()
    {
        Interlocked.Increment(ref _dnsCacheHitsTotal);
    }

    public void RecordDnsLatency(double seconds)
    {
        AddDouble(ref _dnsLatencySum, seconds);
        Interlocked.Increment(ref _dnsLatencyTotalCount);

        for (int i = 0; i < _dnsLatencyBuckets.Length; i++)
        {
            if (seconds <= _dnsLatencyBuckets[i])
            {
                Interlocked.Increment(ref _dnsLatencyCounts[i]);
                break;
            }
        }
    }

    // -----------------------------
    // DHCP Metrics
    // -----------------------------

    public void RecordDhcpLeaseAllocated(DhcpLeaseAllocatedEvent evt)
    {
        Interlocked.Increment(ref _dhcpLeaseAllocationsTotal);
        Interlocked.Increment(ref _dhcpLeasesActive);
    }

    public void RecordDhcpLeaseReleased(DhcpLeaseReleasedEvent evt)
    {
        long current;
        do
        {
            current = Volatile.Read(ref _dhcpLeasesActive);
            if (current <= 0)
            {
                break;
            }
        }
        while (Interlocked.CompareExchange(ref _dhcpLeasesActive, current - 1, current) != current);
    }

    // -----------------------------
    // NTP Metrics
    // -----------------------------

    public void RecordNtpSync(NtpSyncEvent evt)
    {
        Interlocked.Increment(ref _ntpSyncTotal);
        if (!evt.Success)
        {
            Interlocked.Increment(ref _ntpSyncFailuresTotal);
        }

        Volatile.Write(ref _ntpOffsetMs, evt.Offset.TotalMilliseconds);
    }

    // -----------------------------
    // Prometheus Output
    // -----------------------------

    public string RenderPrometheus()
    {
        var sb = new StringBuilder();

        // Snapshot atomic values
        long dnsQueries = Interlocked.Read(ref _dnsQueriesTotal);
        long dnsResponses = Interlocked.Read(ref _dnsResponsesTotal);
        long dnsNxDomain = Interlocked.Read(ref _dnsNxDomainTotal);
        long dnsServFail = Interlocked.Read(ref _dnsServFailTotal);
        long dnsCacheHits = Interlocked.Read(ref _dnsCacheHitsTotal);

        long dnsLatencyCount = Interlocked.Read(ref _dnsLatencyTotalCount);
        double dnsLatencySum = Volatile.Read(ref _dnsLatencySum);

        long dhcpAllocations = Interlocked.Read(ref _dhcpLeaseAllocationsTotal);
        long dhcpActive = Interlocked.Read(ref _dhcpLeasesActive);

        long ntpTotal = Interlocked.Read(ref _ntpSyncTotal);
        long ntpFailures = Interlocked.Read(ref _ntpSyncFailuresTotal);
        double ntpOffset = Volatile.Read(ref _ntpOffsetMs);

        // DNS Counters
        sb.AppendLine("# HELP dns_queries_total Total number of DNS queries.");
        sb.AppendLine("# TYPE dns_queries_total counter");
        sb.AppendLine($"dns_queries_total {dnsQueries}");

        sb.AppendLine("# HELP dns_responses_total Total number of DNS responses.");
        sb.AppendLine("# TYPE dns_responses_total counter");
        sb.AppendLine($"dns_responses_total {dnsResponses}");

        sb.AppendLine("# HELP dns_nxdomain_total Total number of NXDOMAIN responses.");
        sb.AppendLine("# TYPE dns_nxdomain_total counter");
        sb.AppendLine($"dns_nxdomain_total {dnsNxDomain}");

        sb.AppendLine("# HELP dns_servfail_total Total number of SERVFAIL responses.");
        sb.AppendLine("# TYPE dns_servfail_total counter");
        sb.AppendLine($"dns_servfail_total {dnsServFail}");

        sb.AppendLine("# HELP dns_cache_hits_total Total number of DNS cache hits.");
        sb.AppendLine("# TYPE dns_cache_hits_total counter");
        sb.AppendLine($"dns_cache_hits_total {dnsCacheHits}");

        // DNS Latency Histogram
        sb.AppendLine("# HELP dns_latency_seconds DNS query latency in seconds.");
        sb.AppendLine("# TYPE dns_latency_seconds histogram");

        long cumulative = 0;
        for (int i = 0; i < _dnsLatencyBuckets.Length; i++)
        {
            cumulative += Interlocked.Read(ref _dnsLatencyCounts[i]);
            sb.AppendLine($"dns_latency_seconds_bucket{{le=\"{_dnsLatencyBuckets[i]}\"}} {cumulative}");
        }

        sb.AppendLine($"dns_latency_seconds_bucket{{le=\"+Inf\"}} {dnsLatencyCount}");
        sb.AppendLine($"dns_latency_seconds_sum {dnsLatencySum}");
        sb.AppendLine($"dns_latency_seconds_count {dnsLatencyCount}");

        // DHCP
        sb.AppendLine("# HELP dhcp_lease_allocations_total Total number of DHCP lease allocations.");
        sb.AppendLine("# TYPE dhcp_lease_allocations_total counter");
        sb.AppendLine($"dhcp_lease_allocations_total {dhcpAllocations}");

        sb.AppendLine("# HELP dhcp_leases_active Number of active DHCP leases.");
        sb.AppendLine("# TYPE dhcp_leases_active gauge");
        sb.AppendLine($"dhcp_leases_active {dhcpActive}");

        // NTP
        sb.AppendLine("# HELP ntp_sync_total Total number of NTP sync attempts.");
        sb.AppendLine("# TYPE ntp_sync_total counter");
        sb.AppendLine($"ntp_sync_total {ntpTotal}");

        sb.AppendLine("# HELP ntp_sync_failures_total Total number of failed NTP syncs.");
        sb.AppendLine("# TYPE ntp_sync_failures_total counter");
        sb.AppendLine($"ntp_sync_failures_total {ntpFailures}");

        sb.AppendLine("# HELP ntp_offset_ms Last measured NTP offset in milliseconds.");
        sb.AppendLine("# TYPE ntp_offset_ms gauge");
        sb.AppendLine($"ntp_offset_ms {ntpOffset}");

        return sb.ToString();
    }

    private static void AddDouble(ref double location, double value)
    {
        double newWeight, oldWeight;
        do
        {
            oldWeight = location;
            newWeight = oldWeight + value;
        }
        while (Interlocked.CompareExchange(ref location, newWeight, oldWeight) != oldWeight);
    }
}
