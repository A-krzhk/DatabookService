using DatabookService.Application.DTOs;
using DatabookService.Application.DTOs.UpdateDirectoryTypes;
using DatabookService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace DatabookService.Application.Features.DatabookTypes.Update;

public class UpdateFieldCommand
{
    private readonly IDirectoryTypeRepository _repository;
    private readonly ILogger<UpdateFieldCommand> _logger;

    public UpdateFieldCommand(
        IDirectoryTypeRepository repository,
        ILogger<UpdateFieldCommand> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<DirectoryFieldDto> ExecuteAsync(
        Guid directoryTypeId,
        Guid fieldId,
        UpdateFieldDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating field {FieldId} in directory type {DirectoryTypeId}", 
            fieldId, directoryTypeId);

        // Получаем справочник для валидации
        var directoryType = await _repository.GetByIdAsync(directoryTypeId, cancellationToken);
        if (directoryType == null)
        {
            throw new InvalidOperationException($"Directory type with ID '{directoryTypeId}' not found");
        }

        // Получаем поле для обновления
        var field = await _repository.GetFieldByIdAsync(fieldId, cancellationToken);
        if (field == null)
        {
            throw new InvalidOperationException($"Field with ID '{fieldId}' not found");
        }

        // Проверяем, что поле принадлежит этому справочнику
        if (field.DirectoryTypeId != directoryTypeId)
        {
            throw new InvalidOperationException($"Field does not belong to directory type '{directoryTypeId}'");
        }

        // Обновляем поле (валидация происходит в доменной модели)
        field.Update(dto.Name, dto.Order, dto.IsRequired);

        // Сохраняем изменения через репозиторий
        await _repository.UpdateFieldAsync(field, cancellationToken);

        _logger.LogInformation("Field {FieldId} updated successfully", fieldId);

        return new DirectoryFieldDto(
            field.Id,
            field.Name,
            field.ColumnName,
            (int)field.DataType,
            field.IsRequired,
            field.Order,
            field.IsCollection,
            field.ReferenceDirectoryTypeId,
            field.ReferenceDirectoryType?.Name,
            null
        );
    }
}