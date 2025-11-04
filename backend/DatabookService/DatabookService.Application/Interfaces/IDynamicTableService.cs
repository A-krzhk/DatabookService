using DatabookService.Domain.Entities;

namespace DatabookService.Application.Interfaces;

public interface IDynamicTableService
{
    Task CreateTableAsync(DirectoryType directoryType, CancellationToken cancellationToken = default);
    Task DropTableAsync(string tableName, CancellationToken cancellationToken = default);
}