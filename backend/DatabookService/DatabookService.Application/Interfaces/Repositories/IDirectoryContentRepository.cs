using DatabookService.Domain.Entities;

namespace DatabookService.Application.Interfaces.Repositories;

public interface IDirectoryContentRepository
{
    Task<bool> MarkAsDeletedAsync(Guid directoryTypeId, Guid recordId, CancellationToken cancellationToken);
    Task<List<(Guid DirectoryTypeId, Guid RecordId, string TableName)>> GetSoftDeletedOlderThanAsync(TimeSpan age, CancellationToken cancellationToken);
    Task DeletePhysicallyAsync(Guid directoryTypeId, Guid recordId, string tableName, CancellationToken cancellationToken);
}