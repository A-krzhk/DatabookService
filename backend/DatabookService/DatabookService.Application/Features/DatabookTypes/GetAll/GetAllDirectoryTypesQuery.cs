using DatabookService.Application.DTOs;
using DatabookService.Application.Interfaces;
using DatabookService.Application.Interfaces.Repositories;

namespace DatabookService.Application.Features.DatabookTypes.GetAll;

public class GetAllDirectoryTypesQuery
{
    private readonly IDirectoryTypeRepository _repository;
    private readonly IDirectoryCollectionRepository _collectionRepository;

    public GetAllDirectoryTypesQuery(
        IDirectoryTypeRepository repository,
        IDirectoryCollectionRepository collectionRepository)
    {
        _repository = repository;
        _collectionRepository = collectionRepository;
    }

    public async Task<List<DirectoryTypeDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var directoryTypes = await _repository.GetAllAsync(cancellationToken);

        var result = new List<DirectoryTypeDto>();

        foreach (var dt in directoryTypes)
        {
            var fields = new List<DirectoryFieldDto>();

            foreach (var field in dt.Fields)
            {
                List<CollectionItemDto>? collectionData = null;

                // Если поле является коллекцией, загружаем данные из таблицы коллекции
                if (field.IsCollection)
                {
                    collectionData = await _collectionRepository.GetCollectionDataAsync(
                        dt.TableName,
                        field.ColumnName,
                        field.DataType,
                        field.ReferenceDirectoryType?.TableName,
                        cancellationToken);
                }

                fields.Add(new DirectoryFieldDto(
                    field.Id,
                    field.Name,
                    field.ColumnName,
                    (int)field.DataType,
                    field.IsRequired,
                    field.Order,
                    field.IsCollection,
                    field.ReferenceDirectoryTypeId,
                    field.ReferenceDirectoryType?.Name,
                    collectionData,
                    field.EnumValues
                ));
            }

            result.Add(new DirectoryTypeDto(
                dt.Id,
                dt.Name,
                dt.TableName,
                dt.Description,
                fields
            ));
        }

        return result;
    }
}