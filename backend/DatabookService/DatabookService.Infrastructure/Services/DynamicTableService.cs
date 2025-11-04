using System.Text;
using DatabookService.Application.Interfaces;
using DatabookService.Domain.Entities;
using DatabookService.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DatabookService.Infrastructure.Services;

public class DynamicTableService : IDynamicTableService
{
    private readonly DbContext _context;

    public DynamicTableService(DbContext context)
    {
        _context = context;
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

        foreach (var field in directoryType.Fields.OrderBy(f => f.Order))
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
        sb.AppendLine($"CREATE INDEX \"IX_{directoryType.TableName}_IsDeleted\" ON \"{directoryType.TableName}\" (\"IsDeleted\");");

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
}