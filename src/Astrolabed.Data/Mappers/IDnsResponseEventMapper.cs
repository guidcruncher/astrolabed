using Astrolabed.Data.Models;
using Astrolabed.EventBus.Events;

namespace Astrolabed.Data.Mappers;

/// <summary>
/// Defines mapping transformations between database entities, domain record models, and DTOs.
/// </summary>
public interface IDnsResponseEventMapper
{
    /// <summary>
    /// Maps a DNS response event data transfer object to its persistent database entity representation.
    /// </summary>
    /// <param name="dto">The source DNS response event DTO.</param>
    /// <returns>A mapped <see cref="DnsResponseEventEntity"/> instance.</returns>
    DnsResponseEventEntity ToEntity(DnsResponseEventDto dto);

    /// <summary>
    /// Maps a domain-level DNS response event to its persistent database entity representation.
    /// </summary>
    /// <param name="domainEvent">The source DNS response domain event.</param>
    /// <returns>A mapped <see cref="DnsResponseEventEntity"/> instance.</returns>
    DnsResponseEventEntity ToEntity(DnsResponseEvent domainEvent);

    /// <summary>
    /// Maps a persistent DNS response event database entity to its data transfer object representation.
    /// </summary>
    /// <param name="entity">The source DNS response event database entity.</param>
    /// <returns>A mapped <see cref="DnsResponseEventDto"/> instance.</returns>
    DnsResponseEventDto ToDto(DnsResponseEventEntity entity);
}
