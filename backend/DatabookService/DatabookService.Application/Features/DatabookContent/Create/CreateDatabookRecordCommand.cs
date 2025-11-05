using DatabookService.Application.DTOs;
using DatabookService.Application.Features.DatabookTypes.Create;
using DatabookService.Application.Interfaces;
using DatabookService.Application.Interfaces.Repositories;
using DatabookService.Domain.Entities;
using DatabookService.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Threading;

namespace DatabookService.Application.Features.DatabookContent.Create;

public class CreateDatabookRecordCommand
{
    private readonly IDirectoryTypeRepository _directoryTypeRepository;
    private readonly IDynamicTableService _dynamicTableService;
    private readonly ILogger<CreateDatabookRecordCommand> _logger;
    
    public CreateDatabookRecordCommand(
        IDirectoryTypeRepository directoryTypeRepository,
        IDynamicTableService dynamicTableService,
        ILogger<CreateDatabookRecordCommand> logger)
    {
        _directoryTypeRepository = directoryTypeRepository;
        _dynamicTableService = dynamicTableService;
        _logger = logger;
    }

    public async Task<IResult> ExecuteAsync(
        CreateDatabookRecordDto recordDto,
        CancellationToken cancellationToken = default)
    {
        //Получение directoryType
        var directoryType = await _directoryTypeRepository.GetByIdAsync(recordDto.TableId);
        if (directoryType == null)
            throw new ArgumentException("Table with such id is not found.");

        _logger.LogInformation($"Inserting data in table '{directoryType.TableName}'");

        //получение данных всех полей данной таблицы
        var fields = directoryType.Fields;
        if (directoryType == null)
            throw new ArgumentException("Table has no any fields.");

        //Валидация добавляемых полей
        var validationResult = await ValidateFields(directoryType.TableName, fields, recordDto.FieldsValues, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.BadRequest(validationResult.ErrorMessage);
        }

        var result = await _dynamicTableService.InsertValues(directoryType.TableName, fields, recordDto.FieldsValues, cancellationToken);

        if (result == 0)
            return Results.BadRequest();
        return Results.Ok();
    }

    private async Task<ValidationResult> ValidateFields(
        string tableName,
        IReadOnlyCollection<DirectoryField> expectedFields,
        Dictionary<string, object> actualFields,
        CancellationToken cancellationToken = default)
    {
        //Проверка, что в запросе заданы все обязательные поля
        foreach (var field in expectedFields.Where(f => f.IsRequired))  
        {
            if (!actualFields.ContainsKey(field.ColumnName) || actualFields[field.ColumnName] == null) 
                return ValidationResult.Error($"Field {field.ColumnName} is required."); 
        }
        
        //Проверка типов данных и наличия полей в таблице
        foreach (var (fieldName, fieldValue) in actualFields) 
        {
            var expectedField = expectedFields.FirstOrDefault(f => f.ColumnName == fieldName);
            if (expectedField == null)
                return ValidationResult.Error($"Field {fieldName} is not defined.");

            var fieldType = expectedField.DataType;
            if (!(await IsTypeCorrect(tableName, fieldType, fieldValue, cancellationToken))) 
            {
                return ValidationResult.Error($"Field {fieldName} has incorrect data type.");
            }            
        }

        return ValidationResult.Ok();
    }

    private async Task<bool> IsTypeCorrect(
        string tableName,
        FieldDataType fieldType,
        object fieldValue,
        CancellationToken cancellationToken = default)
    {
        //функция для проверки типов
        if (fieldValue == null) return true;

        //Проверка для jsonElement
        if (fieldValue is JsonElement element)
        {
            return fieldType switch
            {
                FieldDataType.String => element.ValueKind == JsonValueKind.String,
                FieldDataType.Number => element.ValueKind == JsonValueKind.Number,
                FieldDataType.Identifier => element.ValueKind == JsonValueKind.String ||
                                          element.ValueKind == JsonValueKind.Number,
                //True, False или 0, 1
                FieldDataType.Checkbox => element.ValueKind == JsonValueKind.True ||
                                          element.ValueKind == JsonValueKind.False ||
                                          (element.ValueKind == JsonValueKind.Number &&
                                           element.TryGetInt32(out int intVal) && (intVal == 0 || intVal == 1)), 
                FieldDataType.Reference => element.ValueKind == JsonValueKind.String ||
                                         element.ValueKind == JsonValueKind.Number,
                _ => false
            };
        }

        //Проверка для обычных типов (на всякий случай)
        return fieldType switch
        {
            FieldDataType.String => fieldValue is string,
            FieldDataType.Number => fieldValue is int or long or decimal or double or float,
            FieldDataType.Identifier => fieldValue is int or long or string,
            FieldDataType.Checkbox => fieldValue is bool,
            FieldDataType.Reference => await _dynamicTableService.IsReferenceCorrect(tableName, fieldValue, cancellationToken),
            _ => false
        };
    }

    //модель для сохранения результата валидации
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }

        public static ValidationResult Ok() => new ValidationResult { IsValid = true };
        public static ValidationResult Error(string message) => new ValidationResult { IsValid = false, ErrorMessage = message };
    }
}