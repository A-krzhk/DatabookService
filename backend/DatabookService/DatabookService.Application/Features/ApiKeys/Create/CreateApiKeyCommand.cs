using DatabookService.Application.DTOs.ApiKeys;
using DatabookService.Application.Interfaces.Repositories;
using DatabookService.Application.Interfaces.Security;
using DatabookService.Domain.Entities;

namespace DatabookService.Application.Features.ApiKeys.Create;

public class CreateApiKeyCommand
{
    private readonly IApiKeyRepository _repo;
    private readonly IApiKeyHasher _hasher;
    private readonly IApiKeyGenerator _generator;

    public CreateApiKeyCommand(IApiKeyRepository repo, IApiKeyHasher hasher, IApiKeyGenerator generator)
    {
        _repo = repo;
        _hasher = hasher;
        _generator = generator;
    }

    public async Task<CreateApiKeyResponse> ExecuteAsync(CreateApiKeyRequest request, CancellationToken ct)
    {
        var plain = _generator.Generate();
        var hash = _hasher.Hash(plain);
        var entity = new ApiKey(request.Name, hash, request.Role, request.ExpiresAt);
        await _repo.AddAsync(entity, ct);
        await _repo.SaveChangesAsync(ct);
        return new CreateApiKeyResponse(entity.Id, entity.Name, entity.Role, plain, entity.ExpiresAt, entity.CreatedAt);
    }
}


