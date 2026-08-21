using Astrolabed.Data.Models;

namespace Astrolabed.Data.Mappers;

/// <summary>
/// Defines mapping transformations between database entities, domain record models, and DTOs.
/// </summary>
public interface IDnsResponseEventMapper
{
    DnsResponseEventEntity ToEntity(CreateDnsResponseEventDto dto);

    DnsResponseEventEntity ToEntity(DnsResponseEvent domainEvent);

    DnsResponseEventDto ToDto(DnsResponseEventEntity entity);

}

