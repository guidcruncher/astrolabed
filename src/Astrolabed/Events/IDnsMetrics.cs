namespace Astrolabed.Events;

public interface IDnsMetrics
{
    void RecordDnsQuery(DnsQueryEvent evt);
    void RecordDnsResponse(DnsResponseEvent evt);
    void RecordDnsCacheHit();
    void RecordDnsLatency(double seconds);
    void RecordUpstreamLatency(DnsUpstreamLatencyEvent evt);
    void RecordRequestResponseLog(DnsRequestResponseLogEvent evt);
}
