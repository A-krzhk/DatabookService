using DatabookService.Application.DTOs;
using DatabookService.Application.Interfaces;
using DatabookService.Domain.Entities;
using DatabookService.Domain.Enums;

namespace DatabookService.Application.Commands;

public class CreateDirectoryTypeCommand
{
    private readonly IDirectoryTypeRepository _repository;
    private readonly IDynamicTableService _dynamicTableService;

    public CreateDirectoryTypeCommand(
        IDirectoryTypeRepository repository,
        IDynamicTableService dynamicTableService)
    {
        _repository = repository;
        _dynamicTableService = dynamicTableService;
    }

    public async Task<DirectoryTypeDto> ExecuteAsync(
        CreateDirectoryTypeDto dto,
        CancellationToken cancellationToken = default)
    {
        // Проверка существования таблицы
        if (await _repository.TableNameExistsAsync(dto.TableName, cancellationToken))
        {
            throw new InvalidOperationException($"Directory type with table name '{dto.TableName}' already exists");
        }

        // Создание доменной сущности
        var directoryType = new DirectoryType(dto.Name, dto.TableName, dto.Description);

        // Добавление полей
        foreach (var fieldDto in dto.Fields.OrderBy(f => f.Order))
        {
            // Проверка существования ссылочного справочника
            if (fieldDto.DataType == (int)FieldDataType.Reference && fieldDto.ReferenceDirectoryTypeId.HasValue)
            {
                var referenceType = await _repository.GetByIdAsync(
                    fieldDto.ReferenceDirectoryTypeId.Value, 
                    cancellationToken);
                
                if (referenceType == null)
                {
                    throw new InvalidOperationException(
                        $"Reference directory type with ID '{fieldDto.ReferenceDirectoryTypeId}' not found");
                }
            }

            var field = new DirectoryField(
                directoryType.Id,
                fieldDto.Name,
                fieldDto.ColumnName,
                (FieldDataType)fieldDto.DataType,
                fieldDto.IsRequired,
                fieldDto.Order,
                fieldDto.ReferenceDirectoryTypeId
            );

            directoryType.AddField(field);
        }

        // Сохранение в БД
        var savedDirectoryType = await _repository.AddAsync(directoryType, cancellationToken);

        // Создание физической таблицы в БД
        await _dynamicTableService.CreateTableAsync(savedDirectoryType, cancellationToken);

        // Маппинг в DTO
        return MapToDto(savedDirectoryType);
    }

    private DirectoryTypeDto MapToDto(DirectoryType directoryType)
    {
        return new DirectoryTypeDto(
            directoryType.Id,
            directoryType.Name,
            directoryType.TableName,
            directoryType.Description,
            directoryType.CreatedAt,
            directoryType.Fields.Select(f => new DirectoryFieldDto(
                f.Id,
                f.Name,
                f.ColumnName,
                (int)f.DataType,
                f.IsRequired,
                f.Order,
                f.ReferenceDirectoryTypeId,
                f.ReferenceDirectoryType?.Name
            )).ToList()
        );
    }
}