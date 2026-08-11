using Astrolabed.Events;

using Microsoft.Extensions.Logging;

namespace Astrolabed.Metrics;

public sealed class MetricsEventConsumer : IEventConsumer
{
    private readonly MetricsRegistry _registry;
    private readonly ILogger<MetricsEventConsumer> _logger;

    public MetricsEventConsumer(
        MetricsRegistry registry,
        ILogger<MetricsEventConsumer> logger)
    {
        _registry = registry;
        _logger = logger;
    }

    public void Consume(EventRecord evt)
    {
        if (evt is null)
        {
            return;
        }

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Consume received event: {EventType}", evt.GetType().Name);
        }

        switch (evt)
        {
            case DnsQueryEvent q:
                _registry.RecordDnsQuery(q);
                break;

            case DnsResponseEvent r:
                _registry.RecordDnsResponse(r);
                break;

            case DnsCacheHitEvent:
                _registry.RecordDnsCacheHit();
                break;

            case DnsLatencyEvent l:
                _registry.RecordDnsLatency(l.Seconds);
                break;

            case DhcpLeaseAllocatedEvent d1:
                _registry.RecordDhcpLeaseAllocated(d1);
                break;

            case DhcpLeaseReleasedEvent d2:
                _registry.RecordDhcpLeaseReleased(d2);
                break;

            case NtpSyncEvent n:
                _registry.RecordNtpSync(n);
                break;

            default:
                _logger.LogWarning("Unknown event type received: {EventType}", evt.GetType().Name);
                break;
        }
    }
}
