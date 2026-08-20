using Astrolabed.EventBus;

namespace Astrolabed.EventBus.Example;

using Microsoft.Extensions.Logging;

/// <summary>
/// Sample event listener registered in a sub-host.
/// </summary>
public sealed class OrderCreatedListener : IEventListener<OrderCreatedEvent>
{
    private readonly ILogger<OrderCreatedListener> _logger;

    public OrderCreatedListener(ILogger<OrderCreatedListener> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ValueTask HandleAsync(EventMessage<OrderCreatedEvent> message, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received OrderCreatedEvent for Order ID: {OrderId} with Amount: {Amount}. Dispatched at {Timestamp}",
            message.Payload.OrderId,
            message.Payload.Amount,
            message.Timestamp);

        return ValueTask.CompletedTask;
    }
}
