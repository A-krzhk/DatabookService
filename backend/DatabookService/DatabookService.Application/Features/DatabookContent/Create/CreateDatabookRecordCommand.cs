using DatabookService.Application.DTOs;
using DatabookService.Application.Features.DatabookTypes.Create;
using DatabookService.Application.Interfaces;
using DatabookService.Application.Interfaces.Repositories;
using DatabookService.Application.Interfaces.Services;
using DatabookService.Domain.Entities;
using DatabookService.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading;

namespace DatabookService.Application.Features.DatabookContent.Create;

public class CreateDatabookRecordCommand
{
    private readonly IDirectoryTypeRepository _directoryTypeRepository;
    private readonly ITypesValidationService _typesValidationService;
    private readonly IDatabookContentService _databookContentService;
    private readonly ILogger<CreateDatabookRecordCommand> _logger;

    public CreateDatabookRecordCommand(
        IDirectoryTypeRepository directoryTypeRepository,
        ITypesValidationService typesValidationService,
        IDatabookContentService databookContentService,
        ILogger<CreateDatabookRecordCommand> logger)
    {
        _directoryTypeRepository = directoryTypeRepository;
        _logger = logger;
        _typesValidationService = typesValidationService;
        _databookContentService = databookContentService;
    }

    public async Task<IResult> ExecuteAsync(
        CreateDatabookRecordDto recordDto,
        CancellationToken cancellationToken = default)
    {
        // Получаем directoryType
        var directoryType = await _directoryTypeRepository.GetByIdAsync(recordDto.TableId);
        if (directoryType == null)
            throw new ArgumentException("Table with such id is not found.");

        _logger.LogInformation($"Inserting data in table '{directoryType.TableName}'");

        // Получаем список полей для данной таблицы
        var fields = directoryType.Fields;
        if (directoryType == null)
            throw new ArgumentException("Table has no any fields.");

        // Проверяем валидность данных
        ValidationResult validationResult = await _typesValidationService.ValidateFields(directoryType.TableName, fields, recordDto.FieldsValues, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.BadRequest(validationResult.ErrorMessage);
        }

        var result = await _databookContentService.InsertValues(directoryType.TableName, fields, recordDto.FieldsValues, cancellationToken);

        if (result == 0)
            return Results.BadRequest();
        return Results.Ok();
    }
}