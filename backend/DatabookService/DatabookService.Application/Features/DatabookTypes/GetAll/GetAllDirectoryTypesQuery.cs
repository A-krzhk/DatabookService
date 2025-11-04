using DatabookService.Application.DTOs;
using DatabookService.Application.Interfaces;

namespace DatabookService.Application.Features.DatabookTypes.GetAll;

public class GetAllDirectoryTypesQuery
{
    private readonly IDirectoryTypeRepository _repository;

    public GetAllDirectoryTypesQuery(IDirectoryTypeRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<DirectoryTypeDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var directoryTypes = await _repository.GetAllAsync(cancellationToken);

        return directoryTypes.Select(dt => new DirectoryTypeDto(
            dt.Id,
            dt.Name,
            dt.TableName,
            dt.Description,
            dt.CreatedAt,
            dt.Fields.Select(f => new DirectoryFieldDto(
                f.Id,
                f.Name,
                f.ColumnName,
                (int)f.DataType,
                f.IsRequired,
                f.Order,
                f.ReferenceDirectoryTypeId,
                f.ReferenceDirectoryType?.Name
            )).ToList()
        )).ToList();
    }
}