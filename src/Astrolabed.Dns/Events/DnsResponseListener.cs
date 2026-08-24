namespace Astrolabed.Dns.Events;

using Astrolabed.Data.Mappers;
using Astrolabed.Data.Models;
using Astrolabed.Data.Repositories;
using Astrolabed.EventBus;
using Astrolabed.EventBus.Events;

using Microsoft.Extensions.Logging;

/// <summary>
/// Event bus subscriber implementation that listens for <see cref="DnsResponseEvent"/> telemetry messages and persists them to storage.
/// </summary>
public sealed class DnsResponseListener : IEventListener<DnsResponseEvent>
{
    /// <inheritdoc />
    public Type MessageType => typeof(DnsResponseEvent);

    private readonly ILogger<DnsResponseListener> _logger;
    private readonly IDnsResponseEventRepository _repository;
    private readonly IDnsResponseEventMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="DnsResponseListener"/> class.
    /// </summary>
    /// <param name="repository">Repository for storing DNS response telemetry entities.</param>
    /// <param name="mapper">Mapper used to convert domain events to persistence entities.</param>
    /// <param name="logger">Structured logging instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when any dependency is <c>null</c>.</exception>
    public DnsResponseListener(
        IDnsResponseEventRepository repository,
        IDnsResponseEventMapper mapper,
        ILogger<DnsResponseListener> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles asynchronous delivery of <see cref="DnsResponseEvent"/> messages from the event bus.
    /// </summary>
    /// <param name="message">The wrapped message container containing the event payload.</param>
    /// <param name="cancellationToken">Cancellation token to signal operation cancellation.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous handling operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is <c>null</c>.</exception>
    public async ValueTask HandleAsync(EventMessage<DnsResponseEvent> message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var payload = message.Payload;
        _logger.LogInformation(
            "Received DnsResponseEvent {Payload}", payload);

        DnsResponseEventEntity entity = _mapper.ToEntity(payload);
        await _repository.AddAsync(entity, cancellationToken);
    }
}
