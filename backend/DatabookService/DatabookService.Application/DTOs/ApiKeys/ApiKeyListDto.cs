using DatabookService.Domain.Enums;

namespace DatabookService.Application.DTOs.ApiKeys;

public record ApiKeyItemDto(
    Guid Id,
    string Name,
    ApiKeyRole Role,
    bool IsActive,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset CreatedAt);



