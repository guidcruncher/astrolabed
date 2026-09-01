namespace Astrolabed.Data.Mappers;

using System.Net;
using System.Text.Json;

using Astrolabed.Data.Models;
using Astrolabed.EventBus.Events;

/// <summary>
/// Concrete mapper providing bidirectional transformations between entities, DTOs, and domain models.
/// </summary>
public sealed class DnsResponseEventMapper : IDnsResponseEventMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
            Id = string.IsNullOrWhiteSpace(dto.Id) ? Guid.NewGuid().ToString("D") : dto.Id,
            StartTimeUtc = dto.StartTimeUtc.ToUnixTimeMilliseconds(),
            ContextId = dto.ContextId,
            QuestionName = dto.QuestionName,
            QuestionType = dto.QuestionType,
            ClientAddress = dto.ClientAddress,
            ClientName = dto.ClientName,
            ResolutionSource = dto.ResolutionSource,
            Rcode = dto.Rcode,
            DurationMs = dto.DurationMs,
            Blocked = dto.Blocked ? 1 : 0,
            Upstream = dto.Upstream,
            AnswerDataJson = dto.AnswerData is not null ? JsonSerializer.Serialize(dto.AnswerData, JsonOptions) : null,
            TtlSeconds = dto.TtlSeconds,
            BlockRuleId = dto.BlockRuleId
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
            ClientAddress = domainEvent.ClientAddress,
            ClientName = domainEvent.ClientName,
            ResolutionSource = domainEvent.ResolutionSource,
            Rcode = domainEvent.Rcode,
            DurationMs = domainEvent.DurationMs,
            Blocked = domainEvent.Blocked ? 1 : 0,
            Upstream = domainEvent.Upstream,
            AnswerDataJson = domainEvent.AnswerData is not null ? JsonSerializer.Serialize(domainEvent.AnswerData, JsonOptions) : null,
            TtlSeconds = domainEvent.TtlSeconds,
            BlockRuleId = domainEvent.BlockRuleId
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

        IReadOnlyList<string>? answerData = null;
        if (!string.IsNullOrWhiteSpace(entity.AnswerDataJson))
        {
            try
            {
                answerData = JsonSerializer.Deserialize<List<string>>(entity.AnswerDataJson, JsonOptions);
            }
            catch (JsonException)
            {
                answerData = null;
            }
        }

        return new DnsResponseEventDto(
            entity.Id,
            DateTimeOffset.FromUnixTimeMilliseconds(entity.StartTimeUtc),
            entity.ContextId,
            entity.QuestionName,
            entity.QuestionType,
            entity.ClientAddress,
            entity.ClientName,
            entity.ResolutionSource,
            entity.Rcode,
            entity.DurationMs,
            entity.Blocked == 1,
            entity.Upstream,
            answerData,
            entity.TtlSeconds,
            entity.BlockRuleId
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
