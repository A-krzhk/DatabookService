using DatabookService.Application.DTOs;
using DatabookService.Domain.Enums;

namespace DatabookService.Application.Interfaces.Repositories;

public interface IDirectoryCollectionRepository
{
    Task<List<CollectionItemDto>> GetCollectionDataAsync(
        string tableName,
        string columnName,
        FieldDataType dataType,
        string? referenceTableName,
        CancellationToken cancellationToken = default);
}

