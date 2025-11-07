using DatabookService.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace DatabookService.Application.Features.DatabookContent.Delete;

public class SoftDeleteDirectoryRecordCommand
{
    private readonly ILogger<SoftDeleteDirectoryRecordCommand> _logger;
    private readonly IDirectoryContentRepository _directoryContentRepository;

    public SoftDeleteDirectoryRecordCommand(
        IDirectoryContentRepository directoryContentRepository,
        ILogger<SoftDeleteDirectoryRecordCommand> logger)
    {
        _logger = logger;
        _directoryContentRepository = directoryContentRepository;
    }

    public async Task ExecuteAsync(
        Guid directoryTypeId,
        Guid recordId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Soft deleting record {RecordId} in directory type {DirectoryTypeId}",
            recordId, directoryTypeId);

        var updated = await _directoryContentRepository.MarkAsDeletedAsync(directoryTypeId, recordId, cancellationToken);

        if (!updated)
        {
            throw new InvalidOperationException($"Record {recordId} not found in directory {directoryTypeId}");
        }

        _logger.LogInformation("Record {RecordId} marked as deleted successfully", recordId);
    }
}