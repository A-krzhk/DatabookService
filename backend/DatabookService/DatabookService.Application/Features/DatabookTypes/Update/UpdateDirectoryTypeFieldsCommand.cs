using DatabookService.Application.DTOs;
using DatabookService.Application.Interfaces;
using DatabookService.Domain.Entities;
using DatabookService.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DatabookService.Application.Features.DatabookTypes.Update;

public class UpdateDirectoryTypeFieldsCommand
{
    private readonly IDirectoryTypeRepository _repository;
    private readonly IDynamicTableService _dynamicTableService;
    private readonly ILogger<UpdateDirectoryTypeFieldsCommand> _logger;

    public UpdateDirectoryTypeFieldsCommand(
        IDirectoryTypeRepository repository,
        IDynamicTableService dynamicTableService,
        ILogger<UpdateDirectoryTypeFieldsCommand> logger)
    {
        _repository = repository;
        _dynamicTableService = dynamicTableService;
        _logger = logger;
    }

    public async Task<DirectoryTypeDto> ExecuteAsync(
        Guid directoryTypeId,
        UpdateDirectoryFieldsDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating fields for Directory Type with ID: {DirectoryTypeId}", directoryTypeId);

        var directoryType = await _repository.GetByIdAsync(directoryTypeId, cancellationToken);
        if (directoryType == null)
        {
            _logger.LogWarning("Directory Type with ID {DirectoryTypeId} not found", directoryTypeId);
            throw new InvalidOperationException($"Directory type with ID '{directoryTypeId}' not found");
        }

        _logger.LogInformation("Directory Type '{Name}' (Table: {TableName}) loaded with {FieldCount} fields",
            directoryType.Name, directoryType.TableName, directoryType.Fields.Count);

        ValidateFields(dto.Fields, directoryType);

        var existingFields = directoryType.Fields.ToDictionary(f => f.Id, f => f);
        var incomingFieldIds = dto.Fields.Where(f => f.Id.HasValue).Select(f => f.Id!.Value).ToHashSet();

        var fieldsToAdd = new List<UpdateDirectoryFieldDto>();
        var fieldsToUpdate = new List<(DirectoryField existing, UpdateDirectoryFieldDto incoming)>();
        var fieldsToDelete = existingFields.Values.Where(f => !incomingFieldIds.Contains(f.Id)).ToList();

        foreach (var incomingField in dto.Fields)
        {
            if (incomingField.Id.HasValue && existingFields.TryGetValue(incomingField.Id.Value, out var existingField))
            {
                ValidateExistingFieldUpdate(existingField, incomingField);
                fieldsToUpdate.Add((existingField, incomingField));
            }
            else
            {
                // Новое поле
                if (string.IsNullOrWhiteSpace(incomingField.ColumnName) ||
                    !incomingField.DataType.HasValue ||
                    !incomingField.IsRequired.HasValue ||
                    !incomingField.IsCollection.HasValue)
                {
                    _logger.LogError("New field missing required properties: ColumnName, DataType, IsRequired, IsCollection");
                    throw new ArgumentException("New fields must specify ColumnName, DataType, IsRequired, and IsCollection");
                }

                fieldsToAdd.Add(incomingField);
            }
        }

        _logger.LogInformation("Changes detected: {AddCount} fields to add, {UpdateCount} fields to update, {DeleteCount} fields to delete",
            fieldsToAdd.Count, fieldsToUpdate.Count, fieldsToDelete.Count);

        // Проверяем ссылочные справочники
        foreach (var fieldToAdd in fieldsToAdd)
        {
            if (fieldToAdd.DataType == (int)FieldDataType.Reference && fieldToAdd.ReferenceDirectoryTypeId.HasValue)
            {
                var referenceType = await _repository.GetByIdAsync(fieldToAdd.ReferenceDirectoryTypeId.Value, cancellationToken);
                if (referenceType == null)
                {
                    _logger.LogError("Reference directory type with ID {ReferenceId} not found for new field",
                        fieldToAdd.ReferenceDirectoryTypeId.Value);
                    throw new InvalidOperationException(
                        $"Reference directory type with ID '{fieldToAdd.ReferenceDirectoryTypeId}' not found");
                }
            }
        }

        // Применяем изменения
        foreach (var fieldToDelete in fieldsToDelete)
        {
            _logger.LogInformation("Removing field '{FieldName}' (Column: {ColumnName}) from Directory Type '{Name}'",
                fieldToDelete.Name, fieldToDelete.ColumnName, directoryType.Name);
            directoryType.RemoveField(fieldToDelete);
        }

        foreach (var (existingField, incomingField) in fieldsToUpdate)
        {
            if (existingField.Name != incomingField.Name)
            {
                _logger.LogInformation("Renaming field '{OldName}' to '{NewName}' (Column: {ColumnName})",
                    existingField.Name, incomingField.Name, existingField.ColumnName);
                existingField.UpdateName(incomingField.Name);
            }

            if (existingField.Order != incomingField.Order)
            {
                _logger.LogInformation("Changing order of field '{FieldName}' from {OldOrder} to {NewOrder}",
                    existingField.Name, existingField.Order, incomingField.Order);
                existingField.UpdateOrder(incomingField.Order);
            }
        }

        foreach (var fieldToAdd in fieldsToAdd.OrderBy(f => f.Order))
        {
            var newField = new DirectoryField(
                directoryType.Id,
                fieldToAdd.Name,
                fieldToAdd.ColumnName!,
                (FieldDataType)fieldToAdd.DataType!.Value,
                fieldToAdd.IsRequired!.Value,
                fieldToAdd.Order,
                fieldToAdd.IsCollection!.Value,
                fieldToAdd.ReferenceDirectoryTypeId
            );

            _logger.LogInformation("Adding new field '{FieldName}' (Column: {ColumnName}, Type: {DataType}, Order: {Order})",
                newField.Name, newField.ColumnName, newField.DataType, newField.Order);

            directoryType.AddField(newField);
        }

        // Обновление таблицы
        try
        {
            foreach (var fieldToDelete in fieldsToDelete)
            {
                _logger.LogInformation("Dropping column '{ColumnName}' from table '{TableName}'",
                    fieldToDelete.ColumnName, directoryType.TableName);
                await _dynamicTableService.DropColumnAsync(directoryType.TableName, fieldToDelete.ColumnName, cancellationToken);
            }

            foreach (var fieldToAdd in fieldsToAdd)
            {
                var newField = directoryType.GetFieldByColumnName(fieldToAdd.ColumnName!);
                if (newField != null)
                {
                    _logger.LogInformation("Adding column '{ColumnName}' to table '{TableName}'",
                        newField.ColumnName, directoryType.TableName);
                    await _dynamicTableService.AddColumnAsync(directoryType, newField, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating database structure for Directory Type '{Name}' (Table: {TableName})",
                directoryType.Name, directoryType.TableName);
            throw;
        }

        directoryType.Update(directoryType.Name, directoryType.Description);
        await _repository.UpdateAsync(directoryType, cancellationToken);

        _logger.LogInformation("Successfully updated fields for Directory Type '{Name}' (ID: {DirectoryTypeId})",
            directoryType.Name, directoryTypeId);

        var updatedDirectoryType = await _repository.GetByIdAsync(directoryTypeId, cancellationToken);
        return MapToDto(updatedDirectoryType!);
    }

    private void ValidateFields(List<UpdateDirectoryFieldDto> fields, DirectoryType directoryType)
    {
        // Проверка на дубликаты Order
        var orderGroups = fields.GroupBy(f => f.Order).Where(g => g.Count() > 1).ToList();
        if (orderGroups.Any())
        {
            var duplicateOrders = string.Join(", ", orderGroups.Select(g => g.Key));
            _logger.LogError("Duplicate Order values found: {Orders}", duplicateOrders);
            throw new ArgumentException($"Duplicate Order values found: {duplicateOrders}");
        }

        // Проверка на дубликаты ColumnName для новых полей
        var newColumnNames = fields
            .Where(f => !f.Id.HasValue && !string.IsNullOrWhiteSpace(f.ColumnName))
            .Select(f => f.ColumnName)
            .ToList();

        var duplicateColumnNames = newColumnNames
            .GroupBy(c => c)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateColumnNames.Any())
        {
            var duplicates = string.Join(", ", duplicateColumnNames);
            _logger.LogError("Duplicate ColumnName values found in new fields: {ColumnNames}", duplicates);
            throw new ArgumentException($"Duplicate ColumnName values found in new fields: {duplicates}");
        }

        // Проверка на конфликт ColumnName с существующими полями
        var existingColumnNames = directoryType.Fields.Select(f => f.ColumnName).ToHashSet();
        var conflictingColumnNames = newColumnNames.Where(c => existingColumnNames.Contains(c)).ToList();

        if (conflictingColumnNames.Any())
        {
            var conflicts = string.Join(", ", conflictingColumnNames);
            _logger.LogError("ColumnName conflicts with existing fields: {ColumnNames}", conflicts);
            throw new ArgumentException($"ColumnName conflicts with existing fields: {conflicts}");
        }

        // Проверка Order >= 0
        if (fields.Any(f => f.Order < 0))
        {
            _logger.LogError("Invalid Order values (< 0) found");
            throw new ArgumentException("Order must be >= 0");
        }
    }

    private void ValidateExistingFieldUpdate(DirectoryField existing, UpdateDirectoryFieldDto incoming)
    {
        // Проверка, что Id не изменяется
        if (incoming.Id.HasValue && incoming.Id.Value != existing.Id)
        {
            _logger.LogError("Attempt to change Id for existing field '{FieldName}' from {OldId} to {NewId}",
                existing.Name, existing.Id, incoming.Id.Value);
            throw new InvalidOperationException($"Cannot change Id for existing field '{existing.Name}'. Field Id must match existing field Id.");
        }

        if (incoming.DataType.HasValue && incoming.DataType.Value != (int)existing.DataType)
        {
            _logger.LogError("Attempt to change DataType for existing field '{FieldName}'", existing.Name);
            throw new InvalidOperationException($"Cannot change DataType for existing field '{existing.Name}'");
        }

        if (!string.IsNullOrEmpty(incoming.ColumnName) && incoming.ColumnName != existing.ColumnName)
        {
            _logger.LogError("Attempt to change ColumnName for existing field '{FieldName}'", existing.Name);
            throw new InvalidOperationException($"Cannot change ColumnName for existing field '{existing.Name}'");
        }

        if (incoming.IsRequired.HasValue && incoming.IsRequired.Value != existing.IsRequired)
        {
            _logger.LogError("Attempt to change IsRequired for existing field '{FieldName}'", existing.Name);
            throw new InvalidOperationException($"Cannot change IsRequired for existing field '{existing.Name}'");
        }

        if (incoming.IsCollection.HasValue && incoming.IsCollection.Value != existing.IsCollection)
        {
            _logger.LogError("Attempt to change IsCollection for existing field '{FieldName}' (ID: {FieldId})",
                existing.Name, existing.Id);
            throw new InvalidOperationException($"Cannot change IsCollection for existing field '{existing.Name}'");
        }

        if (incoming.ReferenceDirectoryTypeId != existing.ReferenceDirectoryTypeId)
        {
            _logger.LogError("Attempt to change ReferenceDirectoryTypeId for existing field '{FieldName}'", existing.Name);
            throw new InvalidOperationException($"Cannot change ReferenceDirectoryTypeId for existing field '{existing.Name}'");
        }
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
                f.IsCollection,
                f.ReferenceDirectoryTypeId,
                f.ReferenceDirectoryType?.Name,
                null
            )).ToList()
        );
    }
}


