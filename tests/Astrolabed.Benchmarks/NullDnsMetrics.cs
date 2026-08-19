using Astrolabed.Events;

namespace Astrolabed.Dns.Benchmarks;

public sealed class NullDnsMetrics : IDnsMetrics
{
    public void RecordDnsQuery(DnsQueryEvent evt) { }
    public void RecordDnsResponse(DnsResponseEvent evt) { }
    public void RecordDnsCacheHit() { }
    public void RecordDnsLatency(double seconds) { }
    public void RecordUpstreamLatency(DnsUpstreamLatencyEvent evt) { }
    public void RecordRequestResponseLog(DnsRequestResponseLogEvent evt) { }
}
