using DatabookService.Domain.Entities;

namespace DatabookService.Application.Interfaces;

public interface IDynamicTableService
{
    Task CreateTableAsync(DirectoryType directoryType, CancellationToken cancellationToken = default);
    Task DropTableAsync(string tableName, CancellationToken cancellationToken = default);
    Task<bool> IsReferenceCorrect(string tableName, object value, CancellationToken cancellationToken = default);
    Task<int> InsertValues(
        string tableName,
        IReadOnlyCollection<DirectoryField> expectedFields,
        Dictionary<string, object> actualFields, 
        CancellationToken cancellationToken = default);
}