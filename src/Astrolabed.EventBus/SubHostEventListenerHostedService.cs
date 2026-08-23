using System.Collections.Concurrent;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Astrolabed.EventBus;

/// <summary>
/// Hosted background service running within sub-hosts that automatically discovers local 
/// dependency injection event listeners and registers them with the central in-process event broker.
/// </summary>
/// <param name="serviceProvider">Root service provider for resolving registered listener descriptors and scopes.</param>
/// <param name="broker">Central event broker instance for handling pub/sub subscriptions.</param>
/// <param name="logger">Structured logger instance for recording subscription lifecycle events.</param>
public sealed partial class SubHostEventListenerHostedService(
    IServiceProvider serviceProvider,
    IInProcEventBroker broker,
    ILogger<SubHostEventListenerHostedService> logger) : IHostedService, IAsyncDisposable
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> GenericSubscriptionMethods = new();

    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly IInProcEventBroker _broker = broker ?? throw new ArgumentNullException(nameof(broker));
    private readonly ILogger<SubHostEventListenerHostedService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly List<BrokerSubscriptionToken> _subscriptions = [];

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        IEnumerable<IEventListenerMarker> markers = _serviceProvider.GetServices<IEventListenerMarker>();
        IEnumerable<Type> messageTypes = markers.Select(m => m.MessageType).Distinct();

        foreach (Type messageType in messageTypes)
        {
            RegisterGenericListener(messageType);
        }

        LogSubHostListenersRegistered(_logger);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await DisposeSubscriptionsAsync().ConfigureAwait(false);
        LogSubHostListenersUnregistered(_logger);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisposeSubscriptionsAsync().ConfigureAwait(false);
    }

    private void RegisterGenericListener(Type messageType)
    {
        MethodInfo genericMethod = GenericSubscriptionMethods.GetOrAdd(messageType, static type =>
        {
            return typeof(SubHostEventListenerHostedService)
                .GetMethod(nameof(CreateSubscriptionToken), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(type);
        });

        var token = (BrokerSubscriptionToken)genericMethod.Invoke(this, null)!;
        _subscriptions.Add(token);
    }

    private BrokerSubscriptionToken CreateSubscriptionToken<T>() where T : notnull
    {
        return _broker.RegisterListener<T>(async (message, ct) =>
        {
            await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
            IEnumerable<IEventListener<T>> listeners = scope.ServiceProvider.GetServices<IEventListener<T>>();

            foreach (IEventListener<T> listener in listeners)
            {
                await listener.HandleAsync(message, ct).ConfigureAwait(false);
            }
        });
    }

    private async ValueTask DisposeSubscriptionsAsync()
    {
        foreach (BrokerSubscriptionToken subscription in _subscriptions)
        {
            await subscription.DisposeAsync().ConfigureAwait(false);
        }

        _subscriptions.Clear();
    }

    [LoggerMessage(
        EventId = 501,
        Level = LogLevel.Information,
        Message = "Sub-host event listeners successfully registered with central broker.")]
    private static partial void LogSubHostListenersRegistered(ILogger logger);

    [LoggerMessage(
        EventId = 502,
        Level = LogLevel.Information,
        Message = "Sub-host event listeners unregistered from central broker.")]
    private static partial void LogSubHostListenersUnregistered(ILogger logger);
}

