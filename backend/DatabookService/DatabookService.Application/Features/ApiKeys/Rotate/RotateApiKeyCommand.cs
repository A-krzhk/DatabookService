using DatabookService.Application.Interfaces.Repositories;
using DatabookService.Application.Interfaces.Security;

namespace DatabookService.Application.Features.ApiKeys.Rotate;

public class RotateApiKeyCommand
{
    private readonly IApiKeyRepository _repo;
    private readonly IApiKeyHasher _hasher;
    private readonly IApiKeyGenerator _generator;

    public RotateApiKeyCommand(IApiKeyRepository repo, IApiKeyHasher hasher, IApiKeyGenerator generator)
    {
        _repo = repo;
        _hasher = hasher;
        _generator = generator;
    }

    public async Task<(bool Found, string? Plain)> ExecuteAsync(Guid id, CancellationToken ct)
    {
        var entity = await _repo.FindByIdAsync(id, ct);
        if (entity is null) return (false, null);

        var plain = _generator.Generate();
        var hash = _hasher.Hash(plain);
        entity.SetNewHash(hash);
        await _repo.SaveChangesAsync(ct);
        return (true, plain);
    }
}




