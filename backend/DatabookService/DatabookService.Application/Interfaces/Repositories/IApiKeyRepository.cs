using DatabookService.Domain.Entities;

namespace DatabookService.Application.Interfaces.Repositories;

public interface IApiKeyRepository
{
    Task AddAsync(ApiKey apiKey, CancellationToken ct);
    Task<ApiKey?> FindByIdAsync(Guid id, CancellationToken ct);
    Task<List<ApiKey>> GetAllAsync(CancellationToken ct);
    Task<List<ApiKey>> GetActiveAsync(CancellationToken ct);
    Task<int> SaveChangesAsync(CancellationToken ct);
}




