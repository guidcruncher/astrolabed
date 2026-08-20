namespace Astrolabed.EventBus;

using Astrolabed.EventBus.Options;

using System.Collections.Concurrent;
using System.Collections.Immutable;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Central in-process event broker responsible for dispatching generic messages to listeners across IHost instances.
/// </summary>
public sealed class InProcEventBroker : IInProcEventBroker
{
    private readonly ConcurrentDictionary<Type, ImmutableArray<Func<object, CancellationToken, ValueTask>>> _subscribers = new();
    private readonly ILogger<InProcEventBroker> _logger;
    private readonly EventBusOptions _options;

    public InProcEventBroker(
        ILogger<InProcEventBroker> logger,
        IOptions<EventBusOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public ValueTask PublishAsync<T>(T payload, CancellationToken cancellationToken = default) where T : notnull
    {
        if (!_subscribers.TryGetValue(typeof(T), out var handlers) || handlers.IsEmpty)
        {
            return ValueTask.CompletedTask;
        }

        var message = EventMessage<T>.Create(payload);

        for (int i = 0; i < handlers.Length; i++)
        {
            var handler = handlers[i];
            ThreadPool.UnsafeQueueUserWorkItem(static state =>
            {
                _ = state.Broker.ExecuteHandlerAsync(state.Handler, state.Message);
            }, (Broker: this, Handler: handler, Message: message), preferLocal: true);
        }

        return ValueTask.CompletedTask;
    }

    private async ValueTask ExecuteHandlerAsync(Func<object, CancellationToken, ValueTask> handler, object message)
    {
        try
        {
            await handler(message, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing event message of type {MessageType}", message.GetType().Name);
            if (!_options.SuppressListenerExceptions)
            {
                throw;
            }
        }
    }

    public IDisposable RegisterListener<T>(Func<EventMessage<T>, CancellationToken, ValueTask> handler) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(handler);

        Func<object, CancellationToken, ValueTask> wrappedHandler = (objMessage, ct) =>
        {
            if (objMessage is EventMessage<T> typedMessage)
            {
                return handler(typedMessage, ct);
            }
            return ValueTask.CompletedTask;
        };

        _subscribers.AddOrUpdate(
            typeof(T),
            _ => [wrappedHandler],
            (_, current) => current.Add(wrappedHandler));

        return new BrokerSubscriptionToken(() =>
        {
            _subscribers.AddOrUpdate(
                typeof(T),
                ImmutableArray<Func<object, CancellationToken, ValueTask>>.Empty,
                (_, current) => current.Remove(wrappedHandler));
        });
    }
}
