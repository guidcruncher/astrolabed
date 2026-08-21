using Astrolabed.Data.Models;
using Astrolabed.EventBus.Events;

namespace Astrolabed.Data.Mappers;

/// <summary>
/// Defines mapping transformations between database entities, domain record models, and DTOs.
/// </summary>
public interface IDnsResponseEventMapper
{
    DnsResponseEventEntity ToEntity(DnsResponseEventDto dto);

    DnsResponseEventEntity ToEntity(DnsResponseEvent domainEvent);

    DnsResponseEventDto ToDto(DnsResponseEventEntity entity);

}

