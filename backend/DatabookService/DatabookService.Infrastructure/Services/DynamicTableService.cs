using DatabookService.Application.Interfaces;
using DatabookService.Domain.Entities;
using DatabookService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
using Npgsql;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Xml.Linq;

namespace DatabookService.Infrastructure.Services;

public class DynamicTableService : IDynamicTableService
{
    private readonly DbContext _context;
    private readonly string _connectionString;

    public DynamicTableService(DbContext context, IConfiguration configuration)
    {
        _context = context;
        _connectionString = configuration.GetConnectionString("DefaultConnection");
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
            sb.AppendLine($"    \"IdField\" UUID NOT NULL REFERENCES \"DirectoryFields\"(\"Id\") ON DELETE CASCADE,");
            sb.AppendLine($"    \"IdRecord\" UUID NOT NULL REFERENCES \"{directoryType.TableName}\"(\"Id\") ON DELETE CASCADE,");

            // Если коллекция состоит из ссылок на другой справочник
            if (collectionField.DataType == FieldDataType.Reference && collectionField.ReferenceDirectoryType != null)
            {
                sb.AppendLine(
                    $"    \"{collectionField.ReferenceDirectoryType.TableName}Id\" UUID NOT NULL REFERENCES \"{collectionField.ReferenceDirectoryType.TableName}\"(\"Id\") ON DELETE CASCADE,");
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
                    FieldDataType.Date => "DATE",
                    FieldDataType.Datetime => "TIMESTAMP",
                    _ => "TEXT"
                };

                sb.AppendLine($"    \"Value\" {valueColumnType} NOT NULL,");
            }
             
            sb.AppendLine("    \"SortOrder\" INTEGER NOT NULL DEFAULT 0");
            sb.AppendLine(");");
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
            FieldDataType.Date => "DATE",
            FieldDataType.Datetime => "TIMESTAMP",
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
}