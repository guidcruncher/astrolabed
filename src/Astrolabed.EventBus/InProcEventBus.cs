using System.Collections.Concurrent;
using System.Collections.Immutable;

using Astrolabed.EventBus.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabed.EventBus;

/// <summary>
/// Central in-process event broker responsible for dispatching generic messages asynchronously 
/// to registered listeners across application components.
/// </summary>
/// <param name="logger">Structured logger instance for recording telemetry and unhandled subscriber exceptions.</param>
/// <param name="options">Configuration options controlling error suppression and event pipeline behaviors.</param>
/// <param name="timeProvider">Optional time provider for generating deterministic event message timestamps.</param>
public sealed partial class InProcEventBroker(
    ILogger<InProcEventBroker> logger,
    IOptions<EventBusOptions> options,
    TimeProvider? timeProvider = null) : IInProcEventBroker
{
    private readonly ConcurrentDictionary<Type, ImmutableArray<Func<object, CancellationToken, ValueTask>>> _subscribers = new();
    private readonly ILogger<InProcEventBroker> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly EventBusOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    /// <inheritdoc />
    public ValueTask PublishAsync<T>(T payload, CancellationToken cancellationToken = default) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (!_subscribers.TryGetValue(typeof(T), out ImmutableArray<Func<object, CancellationToken, ValueTask>> handlers) || handlers.IsEmpty)
        {
            return ValueTask.CompletedTask;
        }

        EventMessage<T> message = EventMessage<T>.Create(payload, _timeProvider);

        for (int i = 0; i < handlers.Length; i++)
        {
            Func<object, CancellationToken, ValueTask> handler = handlers[i];

            ThreadPool.UnsafeQueueUserWorkItem(static state =>
            {
                _ = state.Broker.ExecuteHandlerAsync(state.Handler, state.Message, state.CancellationToken);
            }, (Broker: this, Handler: handler, Message: message, CancellationToken: cancellationToken), preferLocal: true);
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public BrokerSubscriptionToken RegisterListener<T>(Func<EventMessage<T>, CancellationToken, ValueTask> handler) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(handler);

        Type messageType = typeof(T);

        Func<object, CancellationToken, ValueTask> wrappedHandler = (objMessage, ct) =>
        {
            if (objMessage is EventMessage<T> typedMessage)
            {
                return handler(typedMessage, ct);
            }

            return ValueTask.CompletedTask;
        };

        _subscribers.AddOrUpdate(
            messageType,
            _ => [wrappedHandler],
            (_, current) => current.Add(wrappedHandler));

        return new BrokerSubscriptionToken(() =>
        {
            RemoveSubscriber(messageType, wrappedHandler);
        });
    }

    private void RemoveSubscriber(Type messageType, Func<object, CancellationToken, ValueTask> handler)
    {
        while (_subscribers.TryGetValue(messageType, out ImmutableArray<Func<object, CancellationToken, ValueTask>> current))
        {
            ImmutableArray<Func<object, CancellationToken, ValueTask>> updated = current.Remove(handler);

            if (updated.IsEmpty)
            {
                if (_subscribers.TryRemove(new KeyValuePair<Type, ImmutableArray<Func<object, CancellationToken, ValueTask>>>(messageType, current)))
                {
                    break;
                }
            }
            else
            {
                if (_subscribers.TryUpdate(messageType, updated, current))
                {
                    break;
                }
            }
        }
    }

    private async ValueTask ExecuteHandlerAsync(
        Func<object, CancellationToken, ValueTask> handler,
        object message,
        CancellationToken cancellationToken)
    {
        try
        {
            await handler(message, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            string messageTypeName = message.GetType().Name;
            LogEventListenerError(_logger, ex, messageTypeName);

            if (!_options.SuppressListenerExceptions)
            {
                LogExceptionSuppressionDisabled(_logger, messageTypeName);
            }
        }
    }

    [LoggerMessage(
        EventId = 401,
        Level = LogLevel.Error,
        Message = "Error occurred while executing event listener for message type {MessageType}.")]
    private static partial void LogEventListenerError(ILogger logger, Exception exception, string messageType);

    [LoggerMessage(
        EventId = 402,
        Level = LogLevel.Warning,
        Message = "Unhandled exception encountered in subscriber for {MessageType}. Exception suppression is active; continuing execution.")]
    private static partial void LogExceptionSuppressionDisabled(ILogger logger, string messageType);
}
