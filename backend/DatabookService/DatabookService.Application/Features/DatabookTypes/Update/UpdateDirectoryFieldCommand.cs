using DatabookService.Application.DTOs;
using DatabookService.Application.Interfaces;
using DatabookService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DatabookService.Application.Features.DatabookTypes.Update;

public class UpdateDirectoryFieldCommand
{
    private readonly IDirectoryTypeRepository _repository;
    private readonly ILogger<UpdateDirectoryFieldCommand> _logger;

    public UpdateDirectoryFieldCommand(
        IDirectoryTypeRepository repository,
        ILogger<UpdateDirectoryFieldCommand> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<DirectoryTypeDto> ExecuteAsync(
        Guid directoryTypeId,
        Guid fieldId,
        UpdateDirectoryFieldDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating field {FieldId} for Directory Type {DirectoryTypeId}", fieldId, directoryTypeId);

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

        if (field.Name != dto.Name)
        {
            _logger.LogInformation("Updating field name from '{OldName}' to '{NewName}' (Column: {ColumnName})",
                field.Name, dto.Name, field.ColumnName);
            field.UpdateName(dto.Name);
        }

        directoryType.Update(directoryType.Name, directoryType.Description);
        await _repository.UpdateAsync(directoryType, cancellationToken);

        _logger.LogInformation("Successfully updated field {FieldId} for Directory Type {DirectoryTypeId}", fieldId, directoryTypeId);

        var updatedDirectoryType = await _repository.GetByIdAsync(directoryTypeId, cancellationToken);
        return MapToDto(updatedDirectoryType!);
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

