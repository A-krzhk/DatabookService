using DatabookService.Domain.Entities;

namespace DatabookService.Application.Interfaces;

public interface IDirectoryTypeRepository
{
    Task<DirectoryType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DirectoryType?> GetByTableNameAsync(string tableName, CancellationToken cancellationToken = default);
    Task<List<DirectoryType>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<DirectoryType> AddAsync(DirectoryType directoryType, CancellationToken cancellationToken = default);
    Task UpdateAsync(DirectoryType directoryType, CancellationToken cancellationToken = default);
    Task<bool> TableNameExistsAsync(string tableName, CancellationToken cancellationToken = default);
}