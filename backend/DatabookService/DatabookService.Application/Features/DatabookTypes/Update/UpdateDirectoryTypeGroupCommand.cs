using System.Linq;
using DatabookService.Application.DTOs;
using DatabookService.Application.Interfaces;
using DatabookService.Application.Interfaces.Repositories;
using DatabookService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DatabookService.Application.Features.DatabookTypes.Update;

public class UpdateDirectoryTypeGroupCommand
{
    private readonly IDirectoryTypeRepository _directoryTypeRepository;
    private readonly IDirectoryGroupRepository _directoryGroupRepository;
    private readonly ILogger<UpdateDirectoryTypeGroupCommand> _logger;

    public UpdateDirectoryTypeGroupCommand(
        IDirectoryTypeRepository directoryTypeRepository,
        IDirectoryGroupRepository directoryGroupRepository,
        ILogger<UpdateDirectoryTypeGroupCommand> logger)
    {
        _directoryTypeRepository = directoryTypeRepository;
        _directoryGroupRepository = directoryGroupRepository;
        _logger = logger;
    }

    public async Task<DirectoryTypeDto> ExecuteAsync(
        Guid directoryTypeId,
        Guid? directoryGroupId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating group for directory type {DirectoryTypeId}", directoryTypeId);

        var directoryType = await _directoryTypeRepository.GetByIdAsync(directoryTypeId, cancellationToken);
        if (directoryType == null)
        {
            throw new InvalidOperationException($"Directory type with ID '{directoryTypeId}' not found");
        }

        var targetGroupId = directoryGroupId ?? DirectoryGroup.DefaultId;
        if (directoryGroupId.HasValue)
        {
            var group = await _directoryGroupRepository.GetByIdAsync(directoryGroupId.Value, cancellationToken);
            if (group == null)
            {
                throw new InvalidOperationException($"Directory group with ID '{directoryGroupId}' not found");
            }
        }

        directoryType.SetGroup(targetGroupId);
        await _directoryTypeRepository.UpdateAsync(directoryType, cancellationToken);

        var updated = await _directoryTypeRepository.GetByIdAsync(directoryTypeId, cancellationToken)
                      ?? directoryType;

        _logger.LogInformation(
            "Directory type {DirectoryTypeId} group updated to {DirectoryGroupId}",
            directoryTypeId,
            updated.DirectoryGroupId);

        return MapToDto(updated);
    }

    private static DirectoryTypeDto MapToDto(DirectoryType directoryType)
    {
        return new DirectoryTypeDto(
            directoryType.Id,
            directoryType.Name,
            directoryType.TableName,
            directoryType.Description,
            directoryType.DirectoryGroupId,
            directoryType.DirectoryGroup?.Name,
            directoryType.Fields
                .Select(f => new DirectoryFieldDto(
                    f.Id,
                    f.Name,
                    f.ColumnName,
                    (int)f.DataType,
                    f.IsRequired,
                    f.Order,
                    f.IsCollection,
                    f.ReferenceDirectoryTypeId,
                    f.ReferenceDirectoryType?.Name,
                    f.EnumValues,
                    null))
                .ToList()
        );
    }
}
