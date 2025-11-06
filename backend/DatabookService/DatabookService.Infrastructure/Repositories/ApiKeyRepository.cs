using DatabookService.Application.Interfaces.Repositories;
using DatabookService.Domain.Entities;
using DatabookService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DatabookService.Infrastructure.Repositories;

public class ApiKeyRepository : IApiKeyRepository
{
    private readonly ApplicationDbContext _db;

    public ApiKeyRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(ApiKey apiKey, CancellationToken ct)
    {
        await _db.ApiKeys.AddAsync(apiKey, ct);
    }

    public Task<ApiKey?> FindByIdAsync(Guid id, CancellationToken ct)
    {
        return _db.ApiKeys.FirstOrDefaultAsync(k => k.Id == id, ct);
    }

    public Task<List<ApiKey>> GetAllAsync(CancellationToken ct)
    {
        return _db.ApiKeys
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(ct);
    }

    public Task<List<ApiKey>> GetActiveAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        return _db.ApiKeys
            .Where(k => k.IsActive && (!k.ExpiresAt.HasValue || k.ExpiresAt > now))
            .ToListAsync(ct);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct)
    {
        return _db.SaveChangesAsync(ct);
    }
}




