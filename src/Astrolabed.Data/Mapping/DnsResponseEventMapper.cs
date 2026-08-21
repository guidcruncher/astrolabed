using System.Net;

using Astrolabed.Data.Models;
using Astrolabed.Dns.Events;

namespace Astrolabed.Data.Mappers;

/// <summary>
/// Concrete mapper providing bidirectional transformations between entities, DTOs, and domain models.
/// </summary>
public sealed class DnsResponseEventMapper : IDnsResponseEventMapper
{
    public DnsResponseEventEntity ToEntity(CreateDnsResponseEventDto dto)
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

    public DnsResponseEventEntity ToEntity(DnsResponseEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return new DnsResponseEventEntity
        {
            Id = Guid.NewGuid().ToString("D"),
            StartTimeUtc = domainEvent.StartTimeUTC.ToUnixTimeMilliseconds(),
            ContextId = domainEvent.ContextId,
            QuestionName = domainEvent.QuestionName,
            QuestionType = domainEvent.QuestionType.ToString(),
            ClientEndpoint = domainEvent.ClientEndpoint.ToString() ?? string.Empty,
            ClientName = domainEvent.ClientName,
            ResolutionSource = domainEvent.ResolutionSource,
            DurationMs = domainEvent.DurationMs
        };
    }

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
            entity.DurationMs
        );
    }

    private static EndPoint ParseEndPoint(string endpointString)
    {
        if (IPEndPoint.TryParse(endpointString, out var ipEndPoint))
        {
            return ipEndPoint;
        }

        return new DnsEndPoint(endpointString, 0);
    }
}
