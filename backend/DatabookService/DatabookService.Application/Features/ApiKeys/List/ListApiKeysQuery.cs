using DatabookService.Application.DTOs.ApiKeys;
using DatabookService.Application.Interfaces.Repositories;

namespace DatabookService.Application.Features.ApiKeys.List;

public class ListApiKeysQuery
{
    private readonly IApiKeyRepository _repo;

    public ListApiKeysQuery(IApiKeyRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<ApiKeyItemDto>> ExecuteAsync(CancellationToken ct)
    {
        var items = await _repo.GetAllAsync(ct);
        return items.Select(k => new ApiKeyItemDto(k.Id, k.Name, k.Role, k.IsActive, k.ExpiresAt, k.CreatedAt)).ToList();
    }
}


