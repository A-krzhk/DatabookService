using DatabookService.Domain.Entities;

namespace DatabookService.Application.Interfaces.Repositories;

public interface IDirectoryGroupRepository
{
    Task<List<DirectoryGroup>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<DirectoryGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DirectoryGroup> AddAsync(DirectoryGroup group, CancellationToken cancellationToken = default);
    Task UpdateAsync(DirectoryGroup group, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
