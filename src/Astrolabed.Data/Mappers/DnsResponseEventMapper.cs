using System.Net;

using Astrolabed.Data.Models;
using Astrolabed.EventBus.Events;

namespace Astrolabed.Data.Mappers;

/// <summary>
/// Concrete mapper providing bidirectional transformations between entities, DTOs, and domain models.
/// </summary>
public sealed class DnsResponseEventMapper : IDnsResponseEventMapper
{
    /// <summary>
    /// Converts a <see cref="DnsResponseEventDto"/> data transfer object into a persistable <see cref="DnsResponseEventEntity"/>.
    /// </summary>
    /// <param name="dto">The source DTO model to map.</param>
    /// <returns>A mapped <see cref="DnsResponseEventEntity"/> ready for database persistence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dto"/> is <c>null</c>.</exception>
    public DnsResponseEventEntity ToEntity(DnsResponseEventDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new DnsResponseEventEntity
        {
            Id = Guid.NewGuid().ToString("D"),
            StartTimeUtc = dto.StartTimeUtc.ToUnixTimeMilliseconds(),
            ContextId = dto.ContextId,
            QuestionName = dto.QuestionName,
            QuestionType = dto.QuestionType,
            ClientEndpoint = dto.ClientEndpoint,
            ClientName = dto.ClientName,
            ResolutionSource = dto.ResolutionSource,
            DurationMs = dto.DurationMs
        };
    }

    /// <summary>
    /// Converts an in-memory <see cref="DnsResponseEvent"/> domain event into a persistable <see cref="DnsResponseEventEntity"/>.
    /// </summary>
    /// <param name="domainEvent">The source domain event model to map.</param>
    /// <returns>A mapped <see cref="DnsResponseEventEntity"/> record.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="domainEvent"/> is <c>null</c>.</exception>
    public DnsResponseEventEntity ToEntity(DnsResponseEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return new DnsResponseEventEntity
        {
            Id = Guid.NewGuid().ToString("D"),
            StartTimeUtc = domainEvent.StartTimeUTC.ToUnixTimeMilliseconds(),
            ContextId = domainEvent.ContextId,
            QuestionName = domainEvent.QuestionName,
            QuestionType = domainEvent.QuestionType,
            ClientEndpoint = domainEvent.ClientEndpoint.ToString() ?? string.Empty,
            ClientName = domainEvent.ClientName,
            ResolutionSource = domainEvent.ResolutionSource,
            DurationMs = domainEvent.DurationMs,
            Blocked = domainEvent.Blocked ? 1 : 0
        };
    }

    /// <summary>
    /// Converts a <see cref="DnsResponseEventEntity"/> database record into a <see cref="DnsResponseEventDto"/> transport object.
    /// </summary>
    /// <param name="entity">The source entity record to map.</param>
    /// <returns>A mapped <see cref="DnsResponseEventDto"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <c>null</c>.</exception>
    public DnsResponseEventDto ToDto(DnsResponseEventEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new DnsResponseEventDto(
            entity.Id,
            DateTimeOffset.FromUnixTimeMilliseconds(entity.StartTimeUtc),
            entity.ContextId,
            entity.QuestionName,
            entity.QuestionType,
            entity.ClientEndpoint,
            entity.ClientName,
            entity.ResolutionSource,
            entity.DurationMs,
        entity.Blocked == 1
        );
    }

    /// <summary>
    /// Parses a raw endpoint string into an <see cref="IPEndPoint"/> or <see cref="DnsEndPoint"/> fallback.
    /// </summary>
    /// <param name="endpointString">The endpoint string to parse.</param>
    /// <returns>A concrete <see cref="EndPoint"/> instance.</returns>
    private static EndPoint ParseEndPoint(string endpointString)
    {
        if (IPEndPoint.TryParse(endpointString, out var ipEndPoint))
        {
            return ipEndPoint;
        }

        return new DnsEndPoint(endpointString, 0);
    }
}
