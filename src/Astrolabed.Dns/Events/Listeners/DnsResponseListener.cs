namespace Astrolabed.Dns.Events.Listeners;

using Astrolabed.Data.Mapping;
using Astrolabed.Data.Models;
using Astrolabed.Data.Repositories;
using Astrolabed.Dns.Events;
using Astrolabed.EventBus;

using Microsoft.Extensions.Logging;

public sealed class DnsResponseListener : IEventListener<DnsResponseEvent>
{
    private readonly ILogger<DnsResponseListener> _logger;
    private readonly IDnsResponseEventRepository _repository;
    private readonly IDnsResponseEventMapper _mapper;

    public DnsResponseListener(
      IDnsResponseEventRepository repository,
      IDnsResponseEventMapper mapper,
      ILogger<DnsResponseListener> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _repository = repository;
        _mapper = mapper;
    }

    public ValueTask HandleAsync(EventMessage<DnsResponseEvent> message, CancellationToken cancellationToken)
    {
        var payload = message.Payload;
        _logger.LogInformation(
            "Received DnsResponseEvent {Payload}", payload);

        DnsResponseEventEntity entity = _mapper.ToEntity(payload);

        await _repository.AddAsync(entity, cancellationToken);

        return ValueTask.CompletedTask;
    }
}
