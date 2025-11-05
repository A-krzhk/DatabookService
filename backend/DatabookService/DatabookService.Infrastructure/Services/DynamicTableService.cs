using DatabookService.Application.Interfaces;
using DatabookService.Domain.Entities;
using DatabookService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Npgsql;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace DatabookService.Infrastructure.Services;

public class DynamicTableService : IDynamicTableService
{
    private readonly DbContext _context;
    private readonly string _connectionString;

    public DynamicTableService(DbContext context, IConfiguration configuration)
    {
        _context = context;
        _connectionString = configuration.GetConnectionString("DefaultConnection"); ;
    }

    public async Task CreateTableAsync(DirectoryType directoryType, CancellationToken cancellationToken = default)
    {
        var sql = GenerateCreateTableSql(directoryType);
        await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    public async Task DropTableAsync(string tableName, CancellationToken cancellationToken = default)
    {
        var sql = $"DROP TABLE IF EXISTS \"{tableName}\"";
        await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    private string GenerateCreateTableSql(DirectoryType directoryType)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"CREATE TABLE \"{directoryType.TableName}\" (");
        sb.AppendLine("    \"Id\" UUID PRIMARY KEY DEFAULT gen_random_uuid(),");

        // Только поля, которые НЕ являются коллекциями — они идут в основной таблице
        foreach (var field in directoryType.Fields
                     .Where(f => !f.IsCollection)
                     .OrderBy(f => f.Order))
        {
            var columnDefinition = GetColumnDefinition(field);
            sb.AppendLine($"    \"{field.ColumnName}\" {columnDefinition},");
        }

        // Добавляем обязательные поля для soft delete
        sb.AppendLine("    \"IsDeleted\" BOOLEAN NOT NULL DEFAULT FALSE,");
        sb.AppendLine("    \"DeletedDate\" TIMESTAMP NULL,");
        sb.AppendLine("    \"CreatedAt\" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,");
        sb.AppendLine("    \"UpdatedAt\" TIMESTAMP NULL");

        sb.AppendLine(");");

        // Создаем индекс для soft delete
        sb.AppendLine(
            $"CREATE INDEX \"IX_{directoryType.TableName}_IsDeleted\" ON \"{directoryType.TableName}\" (\"IsDeleted\");");

        // === Создаём вспомогательные таблицы для полей, где IsCollection = true ===
        foreach (var collectionField in directoryType.Fields.Where(f => f.IsCollection))
        {
            // Проверка: если коллекция — это ссылки на другой справочник, ReferenceDirectoryType обязателен
            if (collectionField.DataType == FieldDataType.Reference && collectionField.ReferenceDirectoryType == null)
                throw new InvalidOperationException(
                    $"Collection field '{collectionField.ColumnName}' must have a reference directory type.");

            var linkTableName = $"{directoryType.TableName}_{collectionField.ColumnName}";

            sb.AppendLine();
            sb.AppendLine($"CREATE TABLE \"{linkTableName}\" (");
            sb.AppendLine("    \"Id\" UUID PRIMARY KEY DEFAULT gen_random_uuid(),");
            sb.AppendLine(
                $"    \"{directoryType.TableName}Id\" UUID NOT NULL REFERENCES \"{directoryType.TableName}\"(\"Id\") ON DELETE CASCADE,");

            // Если коллекция состоит из ссылок на другой справочник
            if (collectionField.DataType == FieldDataType.Reference && collectionField.ReferenceDirectoryType != null)
            {
                sb.AppendLine(
                    $"    \"{collectionField.ReferenceDirectoryType.TableName}Id\" UUID NOT NULL REFERENCES \"{collectionField.ReferenceDirectoryType.TableName}\"(\"Id\") ON DELETE CASCADE");
            }
            // Если коллекция хранит простые значения (строки, числа и т.п.)
            else
            {
                var valueColumnType = collectionField.DataType switch
                {
                    FieldDataType.String => "VARCHAR(500)",
                    FieldDataType.Number => "NUMERIC",
                    FieldDataType.Checkbox => "BOOLEAN",
                    FieldDataType.Identifier => "UUID",
                    _ => "TEXT"
                };

                sb.AppendLine($"    \"Value\" {valueColumnType} NOT NULL");
            }

            sb.AppendLine(");");

            // Индексы
            sb.AppendLine(
                $"CREATE INDEX \"IX_{linkTableName}_{directoryType.TableName}Id\" ON \"{linkTableName}\" (\"{directoryType.TableName}Id\");");

            if (collectionField.DataType == FieldDataType.Reference && collectionField.ReferenceDirectoryType != null)
            {
                sb.AppendLine(
                    $"CREATE INDEX \"IX_{linkTableName}_{collectionField.ReferenceDirectoryType.TableName}Id\" ON \"{linkTableName}\" (\"{collectionField.ReferenceDirectoryType.TableName}Id\");");
            }
        }

        return sb.ToString();
    }


    private string GetColumnDefinition(DirectoryField field)
    {
        var sqlType = field.DataType switch
        {
            FieldDataType.String => "VARCHAR(500)",
            FieldDataType.Number => "NUMERIC",
            FieldDataType.Identifier => "UUID",
            FieldDataType.Checkbox => "BOOLEAN",
            FieldDataType.Reference => "UUID",
            _ => throw new ArgumentException($"Unknown data type: {field.DataType}")
        };

        var nullable = field.IsRequired ? "NOT NULL" : "NULL";

        var definition = $"{sqlType} {nullable}";

        // Добавляем внешний ключ для ссылочных полей
        if (field.DataType == FieldDataType.Reference && field.ReferenceDirectoryType != null)
        {
            definition += $" REFERENCES \"{field.ReferenceDirectoryType.TableName}\"(\"Id\") ON DELETE SET NULL";
        }

        return definition;
    }

    public async Task<int> InsertValues(
        string tableName,
        IReadOnlyCollection<DirectoryField> expectedFields,
        Dictionary<string, object> actualFields,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        //заполняем только переданные поля (!isRequired могут не передаваться)
        var validFields = expectedFields
                .Where(f => actualFields.ContainsKey(f.ColumnName));

        //выборка полей для добавления
        var columnNames = string.Join(", ", validFields.Select(f => $@"""{f.ColumnName}"""));
        //параметризация в формате "p_ColumnName"  
        var parameterNames = string.Join(", ", validFields.Select(f => $"@p_{f.ColumnName}"));

        var sql = $@"INSERT INTO ""{tableName}"" ({columnNames}) VALUES ({parameterNames})";

        //Вставка реальных значений вместо "p_ColumnName"  
        var parameters = validFields.Select(f =>
            {
                var value = actualFields[f.ColumnName] ?? DBNull.Value;
                value = ConvertJsonToCorrectType(f, value);

                var parameter = new NpgsqlParameter($"@p_{f.ColumnName}", value);
                return parameter;
            }
        ).ToList();

        var result = await _context.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);

        return result;
    }

    //конвертирование типов данных полей из JSON для добавления в БД
    private object ConvertJsonToCorrectType(DirectoryField f, object value) 
    {
        var valueKind = ((JsonElement)value).ValueKind;

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

        return value;
    }

    public async Task<bool> IsReferenceCorrect(
        string tableName,
        object value,
        CancellationToken cancellationToken = default) 
    {
        if (!(value is Guid or string))
            return false;

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
            command.Parameters.AddWithValue("@reference_value", value);

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
}