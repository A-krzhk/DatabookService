using DatabookService.Application.DTOs.GetDatabookRecords;
using DatabookService.Application.Interfaces;
using DatabookService.Application.Interfaces.Services;
using DatabookService.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DatabookService.Application.Features.DatabookContent.Get;

public class GetDatabookRecordByIdQuery
{
    private readonly IDirectoryTypeRepository _directoryTypeRepository;
    private readonly IDatabookContentService _databookContentService;
    private readonly IChangesHistoryRecordService _changesHistoryService;
    private readonly ILogger<GetDatabookRecordByIdQuery> _logger;

    public GetDatabookRecordByIdQuery(
        IDirectoryTypeRepository directoryTypeRepository,
        IDatabookContentService databookContentService,
        IChangesHistoryRecordService changesHistoryService,
        ILogger<GetDatabookRecordByIdQuery> logger)
    {
        _directoryTypeRepository = directoryTypeRepository;
        _databookContentService = databookContentService;
        _changesHistoryService = changesHistoryService;
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
                return Results.NotFound("Directory type not found");
            
            var record = await _databookContentService.GetRecordByIdAsync(
                directoryType.TableName,
                recordId,
                directoryType.Fields,
                cancellationToken: cancellationToken);

            if (record == null)
                return Results.NotFound("Record not found");

            await _changesHistoryService.LogRecordReadAsync(
                       tableId,
                       recordId,
                       directoryType.TableName,
                       record,
                       cancellationToken);

            // Создаем метаданные колонок
            var columns = directoryType.Fields.Select(f => new ColumnMetadataResponse(
                FieldName: f.ColumnName,
                DisplayName: f.Name,
                DataType: f.DataType.ToString(),
                IsCollection: f.IsCollection,
                IsRequired: f.IsRequired,
                MaxLength: f.DataType == FieldDataType.String ? 255 : null,
                Reference: f.ReferenceDirectoryType != null ? new ReferenceInfoResponse(
                    DirectoryTypeId: f.ReferenceDirectoryType.Id,
                    TableName: f.ReferenceDirectoryType.TableName
                ) : null
            )).ToList();

            // Возвращаем структурированный ответ
            var response = new DatabookRecordResponse(record, columns);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting databook record {RecordId} from table {TableId}", recordId, tableId);
            return Results.Problem("Internal server error");
        }
    }
}