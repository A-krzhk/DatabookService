using DatabookService.Domain.Enums;

namespace DatabookService.Application.DTOs.ApiKeys;

public record CreateApiKeyRequest(string Name, ApiKeyRole Role, DateTimeOffset? ExpiresAt);

public record CreateApiKeyResponse(
    Guid Id,
    string Name,
    ApiKeyRole Role,
    string PlainKey,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset CreatedAt);


