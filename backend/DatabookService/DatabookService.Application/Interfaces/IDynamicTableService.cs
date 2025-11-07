using DatabookService.Domain.Entities;

namespace DatabookService.Application.Interfaces;

public interface IDynamicTableService
{
    Task CreateTableAsync(DirectoryType directoryType, CancellationToken cancellationToken = default);
    Task DropTableAsync(string tableName, CancellationToken cancellationToken = default);
    Task AddColumnAsync(string tableName, DirectoryField field, CancellationToken cancellationToken = default);
    Task DropColumnAsync(string tableName, DirectoryField field, CancellationToken cancellationToken = default);
    Task RenameColumnAsync(string tableName, string oldColumnName, string newColumnName, CancellationToken cancellationToken = default);
}