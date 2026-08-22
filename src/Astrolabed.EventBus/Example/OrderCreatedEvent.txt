using Astrolabed.EventBus;

namespace Astrolabed.EventBus.Example;

/// <summary>
/// Sample domain event message payload.
/// </summary>
/// <param name="OrderId">The unique identifier of the order.</param>
/// <param name="Amount">The total monetary amount of the order.</param>
public sealed record OrderCreatedEvent(Guid OrderId, decimal Amount);
