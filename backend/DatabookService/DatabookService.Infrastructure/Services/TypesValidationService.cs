using DatabookService.Application.DTOs;
using DatabookService.Application.Interfaces;
using DatabookService.Application.Interfaces.Services;
using DatabookService.Domain.Entities;
using DatabookService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DatabookService.Infrastructure.Services
{
    public class TypesValidationService : ITypesValidationService
    {
        private readonly string _connectionString;

        public TypesValidationService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        //конвертирование типов данных полей из JSON для добавления в БД
        public object ConvertJsonToCorrectType(DirectoryField f, object value)
        {
            var valueKind = ((JsonElement)value).ValueKind;

            if (f.DataType == FieldDataType.String && valueKind == JsonValueKind.String)
                return ((JsonElement)value).GetString();

            // Преобразуем строки в Guid для Reference полей
            if ((f.DataType == FieldDataType.Reference || f.DataType == FieldDataType.Identifier)
                && valueKind == JsonValueKind.String)
            {
                string stringValue = ((JsonElement)value).GetString();
                if (Guid.TryParse(stringValue, out Guid guidValue))
                    return guidValue;
            }

            if (f.DataType == FieldDataType.Checkbox
                     && (valueKind == JsonValueKind.True
                        || valueKind == JsonValueKind.False))
            {
                bool boolValue = ((JsonElement)value).GetBoolean();
                return boolValue;
            }

            if (f.DataType == FieldDataType.Number
                     && valueKind == JsonValueKind.Number)
            {
                int numValue = ((JsonElement)value).GetInt32();
                return numValue;
            }

            if ((f.DataType == FieldDataType.Date || f.DataType == FieldDataType.Datetime)
                && valueKind == JsonValueKind.String)
            {
                string stringValue = ((JsonElement)value).GetString();
                if (DateTime.TryParse(stringValue, out DateTime dateValue))
                {
                    // Для Date возвращаем только дату, для Datetime - полную дату-время
                    return f.DataType == FieldDataType.Date ? dateValue.Date : dateValue;
                }
            }

            return value;
        }

        public async Task<bool> IsReferenceCorrect(
            string tableName,
            object value,
            CancellationToken cancellationToken = default)
        {
            Guid valueRef = new Guid();
            if (((JsonElement)value).ValueKind != JsonValueKind.String || !Guid.TryParse(((JsonElement)value).GetString(), out valueRef))
                return false;

            Guid test = valueRef;

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            try
            {
                var pkName = await GetPrimaryKeyColumnNameAsync(tableName, connection, cancellationToken); //определение первичного ключа

                if (string.IsNullOrEmpty(pkName))
                {
                    return false; //если не смогли найти первичный ключ в таблице, на которую есть ссылка,то ошибка
                }
                //проверка pk всех записей таблицы, поиск reference
                var sql = $@"SELECT COUNT(*) FROM ""{tableName}"" WHERE ""{pkName}"" = @reference_value";

                await using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("@reference_value", valueRef);

                var count = (long?)await command.ExecuteScalarAsync(cancellationToken);

                return count > 0;
            }
            catch (NpgsqlException e)
            {
                throw new Exception($"Database error while checking the reference {value}");
            }
        }

        private async Task<string> GetPrimaryKeyColumnNameAsync(
            string tableName,
            NpgsqlConnection connection,
            CancellationToken cancellationToken)
        {
            // PostgreSQL системный запрос для получения первичного ключа таблицы
            var sql = @"
        SELECT column_name
        FROM information_schema.key_column_usage
        WHERE table_name = @table_name 
          AND constraint_name IN (
            SELECT constraint_name 
            FROM information_schema.table_constraints 
            WHERE table_name = @table_name 
              AND constraint_type = 'PRIMARY KEY'
          )";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@table_name", tableName);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result?.ToString();
        }


        /// <summary>
        ///
        /// </summary>

        public async Task<ValidationResult> ValidateFields(
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

                if (field.IsCollection && IsEmptyCollection(actualFields[field.ColumnName]))
                    return ValidationResult.Error($"Collection field {field.ColumnName} cannot be empty.");
            }

            //Проверка типов данных и наличия полей в таблице
            foreach (var (fieldName, fieldValue) in actualFields)
            {
                var expectedField = expectedFields.FirstOrDefault(f => f.ColumnName == fieldName);
                if (expectedField == null)
                    return ValidationResult.Error($"Field {fieldName} is not defined.");

                var validationResult = expectedField.IsCollection
                                        ? await ValidateCollectionField(expectedField, fieldValue, cancellationToken)
                                        : await ValidateSingleField(expectedField, fieldValue, cancellationToken);

                if (!validationResult.IsValid)
                    return validationResult;
            }

            return ValidationResult.Ok();
        }

        private bool IsEmptyCollection(object value)
        {
            if (value == null)
                return true;

            return value switch
            {
                JsonElement { ValueKind: JsonValueKind.Array } element => !element.EnumerateArray().Any(),
                JsonElement { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined } => true,
                _ => false // Если это не коллекция, то не считаем пустой
            };
        }

        private async Task<ValidationResult> ValidateSingleField(
            DirectoryField field,
            object fieldValue,
            CancellationToken cancellationToken)
        {
            if (!(await IsTypeCorrect(field, fieldValue, cancellationToken)))
            {
                return ValidationResult.Error($"Field {field.ColumnName} has incorrect data type.");
            }

            return ValidationResult.Ok();
        }

        private async Task<ValidationResult> ValidateCollectionField(
            DirectoryField field,
            object fieldValue,
            CancellationToken cancellationToken)
        {
            // Проверяем, что значение это коллекция
            if (fieldValue is JsonElement element)
            {
                if (element.ValueKind != JsonValueKind.Array)
                    return ValidationResult.Error($"Field {field.ColumnName} must be an array for collection fields.");

                // Валидируем каждый элемент массива
                foreach (var item in element.EnumerateArray())
                {
                    if (!(await IsTypeCorrect(field, item, cancellationToken)))
                    {
                        return ValidationResult.Error($"Field {field.ColumnName} has incorrect data type.");
                    }
                }

                // Проверка на пустую обязательную коллекцию
                if (field.IsRequired && !element.EnumerateArray().Any())
                {
                    return ValidationResult.Error($"Collection field {field.ColumnName} cannot be empty.");
                }
            }
            else
            {
                return ValidationResult.Error($"Field {field.ColumnName} must be a collection.");
            }

            return ValidationResult.Ok();
        }

        private async Task<bool> IsTypeCorrect(
            DirectoryField field,
            object fieldValue,
            CancellationToken cancellationToken = default)
        {
            //функция для проверки типов
            if (field == null) return true;

            //Проверка для jsonElement
            if (fieldValue is JsonElement element)
            {
                return field.DataType switch
                {
                    FieldDataType.String => element.ValueKind == JsonValueKind.String,
                    FieldDataType.Number => element.ValueKind == JsonValueKind.Number,
                    FieldDataType.Identifier => element.ValueKind == JsonValueKind.String ||
                                              element.ValueKind == JsonValueKind.Number,
                    //True, False или 0, 1
                    FieldDataType.Checkbox => element.ValueKind == JsonValueKind.True ||
                                              element.ValueKind == JsonValueKind.False,
                    FieldDataType.Reference => await IsReferenceCorrect(field.ReferenceDirectoryType.TableName, element, cancellationToken),
                    FieldDataType.Date => element.ValueKind == JsonValueKind.String &&
                                         DateTime.TryParse(element.GetString(), out _),
                    FieldDataType.Datetime => element.ValueKind == JsonValueKind.String &&
                                             DateTime.TryParse(element.GetString(), out _),
                    _ => false
                };
            }

            //Проверка для обычных типов (на всякий случай)
            return field.DataType switch
            {
                FieldDataType.String => fieldValue is string,
                FieldDataType.Number => fieldValue is int or long or decimal or double or float,
                FieldDataType.Identifier => fieldValue is int or long or string,
                FieldDataType.Checkbox => fieldValue is bool,
                FieldDataType.Reference => await IsReferenceCorrect(field.ReferenceDirectoryType.TableName, fieldValue, cancellationToken),
                FieldDataType.Date => fieldValue is DateTime,
                FieldDataType.Datetime => fieldValue is DateTime,
                _ => false
            };
        }
    }
}
