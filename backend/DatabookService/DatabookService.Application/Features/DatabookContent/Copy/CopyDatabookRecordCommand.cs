using DatabookService.Application.DTOs;
using DatabookService.Application.Interfaces;
using DatabookService.Application.Interfaces.Services;
using DatabookService.Domain.Entities;
using DatabookService.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabookService.Application.Features.DatabookContent.Copy
{
    public class CopyDatabookRecordCommand
    {
        private readonly IDirectoryTypeRepository _directoryTypeRepository;
        private readonly IDatabookContentService _databookContentService;
        private readonly IChangesHistoryRecordService _changesHistoryService;
        private readonly ILogger<CopyDatabookRecordCommand> _logger;

        public CopyDatabookRecordCommand(
            IDirectoryTypeRepository directoryTypeRepository,
            IDatabookContentService databookContentService,
            IChangesHistoryRecordService changesHistoryService,
            ILogger<CopyDatabookRecordCommand> logger)
        {
            _directoryTypeRepository = directoryTypeRepository;
            _databookContentService = databookContentService;
            _changesHistoryService = changesHistoryService;
            _logger = logger;
        }

        public async Task<IResult> ExecuteAsync(
            CopyDatabookRecordDto copyDto,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Получаем метаданные справочника
                var directoryType = await _directoryTypeRepository.GetByIdAsync(copyDto.DirectoryTypeId, cancellationToken);
                if (directoryType == null)
                    return Results.NotFound("Directory type not found");

                // Получаем исходную запись
                var sourceRecord = await _databookContentService.GetRecordByIdAsync(
                    directoryType.TableName,
                    copyDto.SourceRecordId,
                    directoryType.Fields,
                    cancellationToken: cancellationToken);

                if (sourceRecord == null)
                    return Results.NotFound("Source record not found");

                // Прямая вставка скопированных данных
                var newRecordId = await _databookContentService.InsertCopiedRecordAsync(
                    directoryType,
                    sourceRecord,
                    cancellationToken);

                if (newRecordId == Guid.Empty)
                    return Results.Problem("Failed to create copy");

                // Логируем создание копии
                await _changesHistoryService.LogRecordCreationAsync(
                    true,
                    copyDto.DirectoryTypeId,
                    newRecordId,
                    directoryType.TableName,
                    sourceRecord,
                    cancellationToken);

                return Results.Ok(new
                {
                    Message = "Record copied successfully",
                    NewRecordId = newRecordId,
                    SourceRecordId = copyDto.SourceRecordId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying record {SourceRecordId} from table {TableId}",
                    copyDto.SourceRecordId, copyDto.DirectoryTypeId);
                return Results.Problem("Internal server error");
            }
        }
    }
}
