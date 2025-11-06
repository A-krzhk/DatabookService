using DatabookService.Application.Interfaces.Repositories;

namespace DatabookService.Application.Features.ApiKeys.Revoke;

public class RevokeApiKeyCommand
{
    private readonly IApiKeyRepository _repo;

    public RevokeApiKeyCommand(IApiKeyRepository repo)
    {
        _repo = repo;
    }

    public async Task<bool> ExecuteAsync(Guid id, CancellationToken ct)
    {
        var entity = await _repo.FindByIdAsync(id, ct);
        if (entity is null) return false;
        entity.Revoke();
        await _repo.SaveChangesAsync(ct);
        return true;
    }
}




