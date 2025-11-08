using DatabookService.Application.Interfaces;
using DatabookService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DatabookService.Application.Features.DatabookContent.Update;

public class UpdateDatabookRecordCommand
{
    private readonly IDirectoryTypeRepository _directoryTypeRepository;
    private readonly IDatabookContentService _databookContentService;
    private readonly ITypesValidationService _typesValidationService;
    private readonly IChangesHistoryRecordService _changesHistoryRecordService;
    private readonly ILogger<UpdateDatabookRecordCommand> _logger;

    public UpdateDatabookRecordCommand(
        IDirectoryTypeRepository directoryTypeRepository,
        IDatabookContentService databookContentService,
        ITypesValidationService typesValidationService,
        IChangesHistoryRecordService changesHistoryRecordService,
        ILogger<UpdateDatabookRecordCommand> logger)
    {
        _directoryTypeRepository = directoryTypeRepository;
        _databookContentService = databookContentService;
        _typesValidationService = typesValidationService;
        _changesHistoryRecordService = changesHistoryRecordService;
        _logger = logger;
    }

    public async Task<IResult> ExecuteAsync(
        Guid tableId,
        Guid recordId,
        Dictionary<string, object> fieldsValues,
        CancellationToken cancellationToken = default)
    {
        var payload = fieldsValues ?? new Dictionary<string, object>();

        var directoryType = await _directoryTypeRepository.GetByIdAsync(tableId, cancellationToken);
        if (directoryType == null)
        {
            return Results.NotFound($"Directory type with id '{tableId}' not found.");
        }

        var existingRecord = await _databookContentService.GetRecordByIdAsync(
            directoryType.TableName,
            recordId,
            directoryType.Fields,
            cancellationToken);

        if (existingRecord == null)
        {
            return Results.NotFound($"Record '{recordId}' not found in table '{directoryType.TableName}'.");
        }

        var validation = await _typesValidationService.ValidateFields(
            directoryType.TableName,
            directoryType.Fields,
            payload,
            cancellationToken);

        if (!validation.IsValid)
        {
            return Results.BadRequest(validation.ErrorMessage);
        }

        await _databookContentService.UpdateValues(
            directoryType,
            directoryType.Fields,
            recordId,
            payload,
            cancellationToken);

        var updatedRecord = await _databookContentService.GetRecordByIdAsync(
            directoryType.TableName,
            recordId,
            directoryType.Fields,
            cancellationToken);

        await _changesHistoryRecordService.LogRecordUpdateAsync(
            directoryType.Id,
            recordId,
            directoryType.TableName,
            existingRecord,
            updatedRecord ?? existingRecord,
            cancellationToken);

        return Results.Ok(updatedRecord);
    }
}
