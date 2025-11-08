using DatabookService.Application.DTOs;
using DatabookService.Application.DTOs.UpdateDirectoryTypes;
using DatabookService.Application.Interfaces;
using DatabookService.Application.Interfaces.Services;
using DatabookService.Domain.Entities;
using DatabookService.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DatabookService.Application.Features.DatabookTypes.Update;
public class AddFieldCommand
{
    private readonly IDirectoryTypeRepository _repository;
    private readonly IDynamicTableService _dynamicTableService;
    private readonly ILogger<AddFieldCommand> _logger;

    public AddFieldCommand(
        IDirectoryTypeRepository repository,
        IDynamicTableService dynamicTableService,
        ILogger<AddFieldCommand> logger)
    {
        _repository = repository;
        _dynamicTableService = dynamicTableService;
        _logger = logger;
    }

    public async Task<DirectoryFieldDto> ExecuteAsync(
        Guid directoryTypeId,
        AddFieldDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding field to directory type {DirectoryTypeId}", directoryTypeId);

        // Получаем справочник (без отслеживания для проверки)
        var directoryType = await _repository.GetByIdAsync(directoryTypeId, cancellationToken);
        if (directoryType == null)
        {
            throw new InvalidOperationException($"Directory type with ID '{directoryTypeId}' not found");
        }

        // Проверяем, что колонка с таким именем не существует
        if (directoryType.Fields.Any(f => f.ColumnName == dto.ColumnName))
        {
            throw new InvalidOperationException($"Field with column name '{dto.ColumnName}' already exists");
        }

        // Проверка существования ссылочного справочника
        DirectoryType? referenceType = null;
        if (dto.DataType == (int)FieldDataType.Reference && dto.ReferenceDirectoryTypeId.HasValue)
        {
            referenceType = await _repository.GetByIdAsync(
                dto.ReferenceDirectoryTypeId.Value,
                cancellationToken);

            if (referenceType == null)
            {
                throw new InvalidOperationException(
                    $"Reference directory type with ID '{dto.ReferenceDirectoryTypeId}' not found");
            }
        }

        var requestedDataType = (FieldDataType)dto.DataType;
        if (requestedDataType == FieldDataType.Enum)
        {
            if (dto.EnumValues == null || !dto.EnumValues.Any())
            {
                throw new InvalidOperationException(
                    $"Enum field '{dto.Name}' must have at least one value.");
            }

            if (dto.EnumValues.Any(string.IsNullOrWhiteSpace))
            {
                throw new InvalidOperationException(
                    $"Enum field '{dto.Name}' contains empty values.");
            }

            if (dto.EnumValues.Distinct(StringComparer.OrdinalIgnoreCase).Count() != dto.EnumValues.Count)
            {
                throw new InvalidOperationException(
                    $"Enum field '{dto.Name}' has duplicate values.");
            }
        }
        else if (dto.EnumValues != null && dto.EnumValues.Any())
        {
            throw new InvalidOperationException(
                $"Non-enum field '{dto.Name}' cannot provide EnumValues.");
        }

        // Создаём новое поле
        var field = new DirectoryField(
            directoryTypeId,
            dto.Name,
            dto.ColumnName,
            requestedDataType,
            dto.IsRequired,
            dto.Order,
            dto.IsCollection,
            dto.ReferenceDirectoryTypeId,
            dto.EnumValues
        );

        // ВАЖНО: Порядок операций:
        // 1. Сначала добавляем колонку в физическую таблицу
        await _dynamicTableService.AddColumnAsync(
            directoryType.TableName, 
            field, 
            cancellationToken);

        // 2. Затем сохраняем поле в БД через репозиторий
        // Это избегает проблем с отслеживанием EF
        await _repository.AddFieldAsync(directoryTypeId, field, cancellationToken);

        _logger.LogInformation("Field {FieldName} added successfully", dto.Name);

        return new DirectoryFieldDto(
            field.Id,
            field.Name,
            field.ColumnName,
            (int)field.DataType,
            field.IsRequired,
            field.Order,
            field.IsCollection,
            field.ReferenceDirectoryTypeId,
            referenceType?.Name,
            field.EnumValues,
            null
        );
    }
}
