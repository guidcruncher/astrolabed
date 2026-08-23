namespace Astrolabed.Dns.Events;

using Astrolabed.Data.Mappers;
using Astrolabed.Data.Models;
using Astrolabed.Data.Repositories;
using Astrolabed.EventBus;
using Astrolabed.EventBus.Events;

using Microsoft.Extensions.Logging;

public sealed class DnsResponseListener : IEventListener<DnsResponseEvent>
{

    /// <inheritdoc />
    public Type MessageType => typeof(DnsResponseEvent);

    private readonly ILogger<DnsResponseListener> _logger;
    private readonly IDnsResponseEventRepository _repository;
    private readonly IDnsResponseEventMapper _mapper;

    public DnsResponseListener(
        IDnsResponseEventRepository repository,
        IDnsResponseEventMapper mapper,
        ILogger<DnsResponseListener> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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
