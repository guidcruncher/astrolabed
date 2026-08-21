namespace Astrolabed.Dns.Events.Listeners;

using Astrolabed.Dns.Events;
using Astrolabed.EventBus;

using Microsoft.Extensions.Logging;

public sealed class DnsResponseListener : IEventListener<DnsResponseEvent>
{
    private readonly ILogger<DnsResponseListener> _logger;

    public DnsResponseListener(ILogger<DnsResponseListener> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ValueTask HandleAsync(EventMessage<DnsResponseEvent> message, CancellationToken cancellationToken)
    {
        var payload = message.Payload;
        _logger.LogInformation(
            "Received DnsResponseEvent {Payload}", payload);

        return ValueTask.CompletedTask;
    }
}
