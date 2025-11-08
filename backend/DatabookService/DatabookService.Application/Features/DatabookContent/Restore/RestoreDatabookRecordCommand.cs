using DatabookService.Application.DTOs;
using DatabookService.Application.Interfaces;
using DatabookService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DatabookService.Application.Features.DatabookContent.Restore;

public class RestoreDatabookRecordCommand
    {
        private readonly IDirectoryTypeRepository _directoryTypeRepository;
        private readonly IDatabookContentService _databookContentService;
        private readonly ILogger<RestoreDatabookRecordCommand> _logger;

        public RestoreDatabookRecordCommand(
            IDirectoryTypeRepository directoryTypeRepository,
            IDatabookContentService databookContentService,
            ILogger<RestoreDatabookRecordCommand> logger)
        {
            _directoryTypeRepository = directoryTypeRepository;
            _databookContentService = databookContentService;
            _logger = logger;
        }

        public async Task<IResult> ExecuteAsync(
            Guid tableId,
            Guid recordId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var directoryType = await _directoryTypeRepository.GetByIdAsync(tableId);
                if (directoryType == null)
                {
                    _logger.LogWarning("Directory type with id {TableId} not found", tableId);
                    return Results.NotFound(new RestoreRecordResponse(
                        Success: false,
                        Message: "Directory type not found"
                    ));
                }

                _logger.LogInformation("Attempting to restore record {RecordId} in table {TableName}", 
                    recordId, directoryType.TableName);

                var success = await _databookContentService.RestoreRecordAsync(
                    directoryType.TableName,
                    recordId,
                    cancellationToken);

                if (!success)
                {
                    _logger.LogWarning("Record {RecordId} not found or already restored", recordId);
                    return Results.NotFound(new RestoreRecordResponse(
                        Success: false,
                        Message: "Record not found or already restored"
                    ));
                }

                _logger.LogInformation("Record {RecordId} successfully restored", recordId);
                return Results.Ok(new RestoreRecordResponse(
                    Success: true,
                    Message: "Record successfully restored",
                    RecordId: recordId
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring record {RecordId} in table {TableId}", recordId, tableId);
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal server error"
                );
            }
        }
    }