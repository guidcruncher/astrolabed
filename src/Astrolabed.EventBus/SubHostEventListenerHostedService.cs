namespace Astrolabed.EventBus;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// Hosted service running in sub-hosts to auto-register local DI event listeners with the central broker.
/// </summary>
public sealed class SubHostEventListenerHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IInProcEventBroker _broker;
    private readonly ILogger<SubHostEventListenerHostedService> _logger;
    private readonly List<IDisposable> _subscriptions = [];

    public SubHostEventListenerHostedService(
        IServiceProvider serviceProvider,
        IInProcEventBroker broker,
        ILogger<SubHostEventListenerHostedService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _broker = broker ?? throw new ArgumentNullException(nameof(broker));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var markers = _serviceProvider.GetServices<IEventListenerMarker>();
        var messageTypes = markers.Select(m => m.MessageType).Distinct();

        foreach (var messageType in messageTypes)
        {
            RegisterGenericListener(messageType);
        }

        _logger.LogInformation("Sub-host event listeners successfully registered with central broker.");
        return Task.CompletedTask;
    }

    private void RegisterGenericListener(Type messageType)
    {
        var method = typeof(SubHostEventListenerHostedService)
            .GetMethod(nameof(CreateSubscriptionToken), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .MakeGenericMethod(messageType);

        var token = (IDisposable)method.Invoke(this, null)!;
        _subscriptions.Add(token);
    }

    private IDisposable CreateSubscriptionToken<T>() where T : notnull
    {
        return _broker.RegisterListener<T>(async (message, ct) =>
        {
            using var scope = _serviceProvider.CreateScope();
            var listeners = scope.ServiceProvider.GetServices<IEventListener<T>>();
            foreach (var listener in listeners)
            {
                await listener.HandleAsync(message, ct);
            }
        });
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }
        _subscriptions.Clear();

        _logger.LogInformation("Sub-host event listeners unregistered from central broker.");
        return Task.CompletedTask;
    }
}
