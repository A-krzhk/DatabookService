using DatabookService.Application.DTOs;
using DatabookService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace DatabookService.Application.Features.DatabookTypes.Update;

public class DeleteDirectoryFieldCommand
{
    private readonly IDirectoryTypeRepository _repository;
    private readonly IDynamicTableService _dynamicTableService;
    private readonly ILogger<DeleteDirectoryFieldCommand> _logger;

    public DeleteDirectoryFieldCommand(
        IDirectoryTypeRepository repository,
        IDynamicTableService dynamicTableService,
        ILogger<DeleteDirectoryFieldCommand> logger)
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
        _logger.LogInformation("Deleting field {FieldId} from Directory Type {DirectoryTypeId}", fieldId, directoryTypeId);

        var directoryType = await _repository.GetByIdAsync(directoryTypeId, cancellationToken);
        if (directoryType == null)
        {
            _logger.LogWarning("Directory Type with ID {DirectoryTypeId} not found", directoryTypeId);
            throw new InvalidOperationException($"Directory type with ID '{directoryTypeId}' not found");
        }

        var field = directoryType.GetFieldById(fieldId);
        if (field == null)
        {
            _logger.LogWarning("Field with ID {FieldId} not found in Directory Type {DirectoryTypeId}", fieldId, directoryTypeId);
            throw new InvalidOperationException($"Field with ID '{fieldId}' not found");
        }

        _logger.LogInformation("Removing field '{FieldName}' (Column: {ColumnName}) from Directory Type '{Name}'",
            field.Name, field.ColumnName, directoryType.Name);

        // Удаление из БД
        try
        {
            _logger.LogInformation("Dropping column '{ColumnName}' from table '{TableName}'",
                field.ColumnName, directoryType.TableName);
            await _dynamicTableService.DropColumnAsync(directoryType.TableName, field.ColumnName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dropping column from database for Directory Type '{Name}' (Table: {TableName})",
                directoryType.Name, directoryType.TableName);
            throw;
        }

        directoryType.RemoveField(field);
        directoryType.Update(directoryType.Name, directoryType.Description);
        await _repository.UpdateAsync(directoryType, cancellationToken);

        _logger.LogInformation("Successfully deleted field {FieldId} from Directory Type {DirectoryTypeId}", fieldId, directoryTypeId);
    }
}


