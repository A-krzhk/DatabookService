using DatabookService.Application.Interfaces;
using DatabookService.Application.Interfaces.Repositories;
using DatabookService.Application.Interfaces.Services;
using DatabookService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DatabookService.Application.Features.DatabookContent.Delete;

public class SoftDeleteDirectoryRecordCommand
{
    private readonly ILogger<SoftDeleteDirectoryRecordCommand> _logger;
    private readonly IDirectoryContentRepository _directoryContentRepository;
    private readonly IChangesHistoryRecordService _changesHistoryService;
    private readonly IDirectoryTypeRepository _directoryTypeRepository;

    public SoftDeleteDirectoryRecordCommand(
        IDirectoryContentRepository directoryContentRepository,
        IChangesHistoryRecordService changesHistoryService,
        IDirectoryTypeRepository directoryTypeRepository,
        ILogger<SoftDeleteDirectoryRecordCommand> logger)
    {
        _logger = logger;
        _directoryContentRepository = directoryContentRepository;
        _changesHistoryService = changesHistoryService;
        _directoryTypeRepository = directoryTypeRepository;
    }

    public async Task ExecuteAsync(
        Guid directoryTypeId,
        Guid recordId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Soft deleting record {RecordId} in directory type {DirectoryTypeId}",
            recordId, directoryTypeId);

        // Получаем directoryType для получения информации для записи в историю
        var directoryType = await _directoryTypeRepository.GetByIdAsync(directoryTypeId);

        var updated = await _directoryContentRepository.MarkAsDeletedAsync(directoryTypeId, recordId, cancellationToken);

        if (!updated)
        {
            throw new InvalidOperationException($"Record {recordId} not found in directory {directoryTypeId}");
        }

        //Получение значений удалённых полей
        var oldValues = await _directoryContentRepository.GetRecordByIdAsync(
               directoryTypeId,
               recordId,
               cancellationToken);

        //Добавление информации в историю записи
        await _changesHistoryService.LogRecordDeletionAsync(
                        directoryTypeId,
                        recordId,
                        directoryType.TableName,
                        oldValues,
                        cancellationToken);

        _logger.LogInformation("Record {RecordId} marked as deleted successfully", recordId);
    }
}