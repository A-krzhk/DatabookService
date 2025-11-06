using DatabookService.Domain.Entities;

namespace DatabookService.Application.Interfaces;

public interface IDynamicTableService
{
    Task CreateTableAsync(DirectoryType directoryType, CancellationToken cancellationToken = default);
    Task DropTableAsync(string tableName, CancellationToken cancellationToken = default);
    Task AddColumnAsync(DirectoryType directoryType, DirectoryField field, CancellationToken cancellationToken = default);
    Task DropColumnAsync(string tableName, string columnName, CancellationToken cancellationToken = default);
}