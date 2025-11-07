using DatabookService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace DatabookService.Application.Features.DatabookTypes.Update;

public class RemoveFieldCommand
{
    private readonly IDirectoryTypeRepository _repository;
    private readonly IDynamicTableService _dynamicTableService;
    private readonly ILogger<RemoveFieldCommand> _logger;

    public RemoveFieldCommand(
        IDirectoryTypeRepository repository,
        IDynamicTableService dynamicTableService,
        ILogger<RemoveFieldCommand> logger)
    {
        _repository = repository;
        _dynamicTableService = dynamicTableService;
        _logger = logger;
    }

    public async Task ExecuteAsync(
        Guid directoryTypeId,
        Guid fieldId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Removing field {FieldId} from directory type {DirectoryTypeId}", 
            fieldId, directoryTypeId);

        // Получаем справочник для валидации
        var directoryType = await _repository.GetByIdAsync(directoryTypeId, cancellationToken);
        if (directoryType == null)
        {
            throw new InvalidOperationException($"Directory type with ID '{directoryTypeId}' not found");
        }

        // Получаем поле для удаления
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

        // ВАЖНО: Порядок операций:
        // 1. Сначала удаляем колонку из физической таблицы
        await _dynamicTableService.DropColumnAsync(directoryType.TableName, field, cancellationToken);

        // 2. Затем удаляем поле из БД через репозиторий
        await _repository.RemoveFieldAsync(fieldId, cancellationToken);

        _logger.LogInformation("Field {FieldId} removed successfully", fieldId);
    }
}