using DatabookService.Application.Interfaces.Services;
using DatabookService.Domain.Entities;
using DatabookService.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Text.Json;

public class DatabookContentService : IDatabookContentService
{
    private readonly string _connectionString;
    private readonly ITypesValidationService _typesValidationService;

        public DatabookContentService(
            IConfiguration configuration,
            ITypesValidationService typesValidationService) 
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _typesValidationService = typesValidationService;
        }

        public async Task<Guid?> InsertValues(
            DirectoryType table,
            IReadOnlyCollection<DirectoryField> expectedFields,
            Dictionary<string, object> actualFields,
            CancellationToken cancellationToken = default)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            int result = 0;
            try
            {
                // Вставляем основную запись (Не коллекционные поля)
                var mainFields = expectedFields
                                 .Where(f => !f.IsCollection && actualFields.ContainsKey(f.ColumnName));
                Guid? recordId = null;
                recordId = (Guid)await InsertMainRecord(table.TableName, expectedFields, actualFields, connection, transaction, cancellationToken);

            // Вставляем коллекционные поля (если есть)
            var collectionFields = expectedFields
                .Where(f => f.IsCollection && actualFields.ContainsKey(f.ColumnName));

                //Если полей-коллекций нет, то и вставляться ничего не будет
                foreach (var field in collectionFields)
                {
                    var values = actualFields[field.ColumnName];
                    await InsertCollectionItems(table.TableName, (Guid)recordId, field, values, connection, transaction, cancellationToken);
                }
                
                await transaction.CommitAsync(cancellationToken);               
                return recordId;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

    private async Task<Guid?> InsertMainRecord(
        string tableName,
        IReadOnlyCollection<DirectoryField> expectedFields,
        Dictionary<string, object> actualFields,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var regularFields = expectedFields
            .Where(f => !f.IsCollection && actualFields.ContainsKey(f.ColumnName))
            .ToList();

        string sql;
        object result;

        if (regularFields.Any())
        {
            var columnNames = string.Join(", ", regularFields.Select(f => $@"""{f.ColumnName}"""));
            var parameterNames = string.Join(", ", regularFields.Select(f => $"@p_{f.ColumnName}"));

            sql = $@"INSERT INTO ""{tableName}"" ({columnNames}) VALUES ({parameterNames}) RETURNING ""Id""";

            var parameters = regularFields.Select(f =>
            {
                var value = _typesValidationService.ConvertJsonToCorrectType(f,
                    actualFields[f.ColumnName] ?? DBNull.Value);
                return new NpgsqlParameter($"@p_{f.ColumnName}", value);
            }).ToArray();

            await using var command = new NpgsqlCommand(sql, connection, transaction);
            command.Parameters.AddRange(parameters);
            result = await command.ExecuteScalarAsync(cancellationToken);
        }
        else
        {
            sql = $@"INSERT INTO ""{tableName}"" (""Id"") VALUES (gen_random_uuid()) RETURNING ""Id""";
            await using var command = new NpgsqlCommand(sql, connection, transaction);
            result = await command.ExecuteScalarAsync(cancellationToken);
        }

        return result as Guid?;
    }

    private async Task InsertCollectionItems(
        string tableName,
        Guid mainRecordId,
        DirectoryField field,
        object values,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var collectionTableName = $"{tableName}_{field.ColumnName}";

        if (values is JsonElement element && element.ValueKind == JsonValueKind.Array)
        {
            var array = element.EnumerateArray();
            int index = 0;
            foreach (var item in array)
            {
                await InsertCollectionItem(collectionTableName, mainRecordId, field, item, index++, connection,
                    transaction, cancellationToken);
            }
        }
    }

    private async Task InsertCollectionItem(
        string collectionTableName,
        Guid mainRecordId,
        DirectoryField field,
        object value,
        int sortOrder,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var valueColumnName = field.DataType == FieldDataType.Reference
            ? $"{field.ReferenceDirectoryType.TableName}Id"
            : "Value";

        var sql = $@"INSERT INTO ""{collectionTableName}"" 
                         (""IdField"", ""IdRecord"", ""{valueColumnName}"", ""SortOrder"")
                         VALUES (@id_field, @id_record, @value, @sort_order)";

        var dbValue = _typesValidationService.ConvertJsonToCorrectType(field, value);

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("@id_field", field.Id);
        command.Parameters.AddWithValue("@id_record", mainRecordId);
        command.Parameters.AddWithValue("@value", dbValue ?? DBNull.Value);
        command.Parameters.AddWithValue("@sort_order", sortOrder);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<List<Dictionary<string, object>>> GetAllRecordsAsync(
        string tableName,
        IReadOnlyCollection<DirectoryField> fields,
        int? pageNumber = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Основная таблица
        var regularFields = fields.Where(f => !f.IsCollection).ToList();
        var columnNames = string.Join(", ", regularFields.Select(f => $"\"{f.ColumnName}\""));

        var sql = $"SELECT \"Id\", {columnNames} FROM \"{tableName}\" WHERE \"IsDeleted\" = FALSE";

        // Добавляем пагинацию, если указаны параметры
        if (pageNumber.HasValue && pageSize.HasValue)
        {
            var offset = (pageNumber.Value - 1) * pageSize.Value;
            sql += $" ORDER BY \"Id\" LIMIT {pageSize} OFFSET {offset}";
        }

        var result = new List<Dictionary<string, object>>();
        await using (var command = new NpgsqlCommand(sql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null! : reader.GetValue(i);
                result.Add(row);
            }
        }

        // Теперь подгружаем коллекционные поля
        var collectionFields = fields.Where(f => f.IsCollection).ToList();
        foreach (var record in result)
        {
            var id = (Guid)record["Id"];
            foreach (var field in collectionFields)
            {
                var collectionTableName = $"{tableName}_{field.ColumnName}";
                var sqlCollection = "";

                if (field.DataType == FieldDataType.Reference)
                {
                    sqlCollection =
                        $@"SELECT ""{field.ReferenceDirectoryType.TableName}Id"" FROM ""{collectionTableName}""
                               WHERE ""IdRecord"" = @id ORDER BY ""SortOrder""";
                }
                else
                {
                    sqlCollection = $@"SELECT ""Value"" FROM ""{collectionTableName}""
                               WHERE ""IdRecord"" = @id ORDER BY ""SortOrder""";
                }

                await using var commandCollection = new NpgsqlCommand(sqlCollection, connection);
                commandCollection.Parameters.AddWithValue("@id", id);

                var values = new List<object>();
                await using var readerCollection = await commandCollection.ExecuteReaderAsync(cancellationToken);
                while (await readerCollection.ReadAsync(cancellationToken))
                {
                    values.Add(readerCollection.IsDBNull(0) ? null! : readerCollection.GetValue(0));
                }

                await readerCollection.CloseAsync();
                record[field.ColumnName] = values;
            }
        }

        return result;
    }


    public async Task<Dictionary<string, object>?> GetRecordByIdAsync(
        string tableName,
        Guid id,
        IReadOnlyCollection<DirectoryField> fields,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var regularFields = fields.Where(f => !f.IsCollection).ToList();
        var columnNames = string.Join(", ", regularFields.Select(f => $"\"{f.ColumnName}\""));
        var sql = $"SELECT \"Id\", {columnNames} FROM \"{tableName}\" WHERE \"Id\" = @id AND \"IsDeleted\" = FALSE";

        var result = new Dictionary<string, object>();
        await using (var command = new NpgsqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("@id", id);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return null;

            for (int i = 0; i < reader.FieldCount; i++)
                result[reader.GetName(i)] = reader.IsDBNull(i) ? null! : reader.GetValue(i);
        }

        // Загрузка коллекций
        var collectionFields = fields.Where(f => f.IsCollection).ToList();
        foreach (var field in collectionFields)
        {
            var collectionTableName = $"{tableName}_{field.ColumnName}";
            var valueColumnName = field.DataType == FieldDataType.Reference
                ? $"{field.ReferenceDirectoryType.TableName}Id"
                : "Value";

            var sqlCollection = $@"SELECT ""{valueColumnName}"" 
                                       FROM ""{collectionTableName}"" 
                                       WHERE ""IdRecord"" = @id ORDER BY ""SortOrder""";

            await using var commandCollection = new NpgsqlCommand(sqlCollection, connection);
            commandCollection.Parameters.AddWithValue("@id", id);

            var values = new List<object>();
            await using var readerCollection = await commandCollection.ExecuteReaderAsync(cancellationToken);
            while (await readerCollection.ReadAsync(cancellationToken))
                values.Add(readerCollection.IsDBNull(0) ? null! : readerCollection.GetValue(0));

            await readerCollection.CloseAsync();
            result[field.ColumnName] = values;
        }

        return result;
    }

    public async Task<int> GetTotalCountAsync(string tableName, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var countSql = $"SELECT COUNT(*) FROM \"{tableName}\" WHERE \"IsDeleted\" = FALSE";
        await using var countCommand = new NpgsqlCommand(countSql, connection);

        var result = await countCommand.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task<List<Dictionary<string, object>>> GetAllDeletedRecordsAsync(
        string tableName,
        IReadOnlyCollection<DirectoryField> fields,
        int? pageNumber = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var regularFields = fields.Where(f => !f.IsCollection).ToList();
        var collectionFields = fields.Where(f => f.IsCollection).ToList();

        // Формируем список колонок
        var columns = new List<string> { "\"Id\"", "\"CreatedAt\"", "\"UpdatedAt\"", "\"DeletedDate\"" };
        if (regularFields.Any())
        {
            columns.AddRange(regularFields.Select(f => $"\"{f.ColumnName}\""));
        }

        var columnNames = string.Join(", ", columns);
        var sql = $@"SELECT {columnNames} 
                        FROM ""{tableName}"" 
                        WHERE ""IsDeleted"" = TRUE
                        ORDER BY ""DeletedDate"" DESC";

        // Добавляем пагинацию
        if (pageNumber.HasValue && pageSize.HasValue)
        {
            var offset = (pageNumber.Value - 1) * pageSize.Value;
            sql += $" LIMIT {pageSize.Value} OFFSET {offset}";
        }

        var result = new List<Dictionary<string, object>>();

        await using (var command = new NpgsqlCommand(sql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, object>();

                // Читаем служебные поля
                row["Id"] = reader.GetValue(reader.GetOrdinal("Id"));
                row["CreatedAt"] = reader.GetValue(reader.GetOrdinal("CreatedAt"));
                row["UpdatedAt"] = reader.IsDBNull(reader.GetOrdinal("UpdatedAt"))
                    ? null!
                    : reader.GetValue(reader.GetOrdinal("UpdatedAt"));
                row["DeletedDate"] = reader.IsDBNull(reader.GetOrdinal("DeletedDate"))
                    ? null!
                    : reader.GetValue(reader.GetOrdinal("DeletedDate"));

                // Читаем обычные поля
                foreach (var field in regularFields)
                {
                    var ordinal = reader.GetOrdinal(field.ColumnName);
                    row[field.ColumnName] = reader.IsDBNull(ordinal) ? null! : reader.GetValue(ordinal);
                }

                result.Add(row);
            }
        }

        // Подгружаем коллекционные поля
        foreach (var record in result)
        {
            var id = (Guid)record["Id"];
            foreach (var field in collectionFields)
            {
                var collectionTableName = $"{tableName}_{field.ColumnName}";
                var valueColumnName = field.DataType == FieldDataType.Reference
                    ? $"{field.ReferenceDirectoryType.TableName}Id"
                    : "Value";

                var sqlCollection = $@"SELECT ""{valueColumnName}"" 
                                          FROM ""{collectionTableName}""
                                          WHERE ""IdRecord"" = @id 
                                          ORDER BY ""SortOrder""";

                await using var commandCollection = new NpgsqlCommand(sqlCollection, connection);
                commandCollection.Parameters.AddWithValue("@id", id);

                var values = new List<object>();
                await using var readerCollection = await commandCollection.ExecuteReaderAsync(cancellationToken);
                while (await readerCollection.ReadAsync(cancellationToken))
                {
                    values.Add(readerCollection.IsDBNull(0) ? null! : readerCollection.GetValue(0));
                }

                await readerCollection.CloseAsync();
                record[field.ColumnName] = values;
            }
        }

        return result;
    }

    public async Task<int> GetTotalDeletedCountAsync(
        string tableName,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = $@"SELECT COUNT(*) 
                        FROM ""{tableName}"" 
                        WHERE ""IsDeleted"" = TRUE";

        await using var command = new NpgsqlCommand(sql, connection);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }


    public async Task<bool> RestoreRecordAsync(
        string tableName,
        Guid recordId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = $@"UPDATE ""{tableName}""
                        SET ""IsDeleted"" = FALSE,
                            ""DeletedDate"" = NULL,
                            ""UpdatedAt"" = NOW()
                        WHERE ""Id"" = @id 
                        AND ""IsDeleted"" = TRUE";

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", recordId);

        var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
        return affectedRows > 0;
    }

    public async Task<Guid> InsertCopiedRecordAsync(
            DirectoryType directoryType,
            Dictionary<string, object> sourceRecord,
            CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            //Вставляем основную запись
            var mainRecordId = await InsertMainRecordAsync(
                directoryType.TableName,
                directoryType.Fields,
                sourceRecord,
                connection,
                transaction,
                cancellationToken);

            if (mainRecordId == Guid.Empty)
                return Guid.Empty;

            //Вставляем коллекции
            await InsertCollectionRecordsAsync(
                directoryType.TableName,
                mainRecordId,
                directoryType.Fields,
                sourceRecord,
                connection,
                transaction,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return mainRecordId;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<Guid> InsertMainRecordAsync(
        string tableName,
        IReadOnlyCollection<DirectoryField> fields,
        Dictionary<string, object> sourceRecord,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        // Фильтруем только обычные поля (не коллекции)
        var regularFields = fields.Where(f => !f.IsCollection).ToList();

        // Формируем SQL для вставки
        var columnNames = string.Join(", ", regularFields.Select(f => $@"""{f.ColumnName}"""));
        var parameterNames = string.Join(", ", regularFields.Select(f => $"@p_{f.ColumnName}"));

        var sql = $@"INSERT INTO ""{tableName}"" ({columnNames}) VALUES ({parameterNames}) RETURNING ""Id""";

        await using var command = new NpgsqlCommand(sql, connection, transaction);

        // Добавляем параметры из sourceRecord
        foreach (var field in regularFields)
        {
            if (sourceRecord.TryGetValue(field.ColumnName, out var value))
            {
                command.Parameters.AddWithValue($"@p_{field.ColumnName}", value ?? DBNull.Value);
            }
            else
            {
                // Если поля нет в sourceRecord, используем NULL
                command.Parameters.AddWithValue($"@p_{field.ColumnName}", DBNull.Value);
            }
        }

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result as Guid? ?? Guid.Empty;
    }

    private async Task InsertCollectionRecordsAsync(
        string tableName,
        Guid mainRecordId,
        IReadOnlyCollection<DirectoryField> fields,
        Dictionary<string, object> sourceRecord,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var collectionFields = fields.Where(f => f.IsCollection).ToList();

        foreach (var field in collectionFields)
        {
            if (sourceRecord.TryGetValue(field.ColumnName, out var collectionData))
            {
                await InsertCollectionItemsAsync(
                    tableName,
                    mainRecordId,
                    field,
                    collectionData,
                    connection,
                    transaction,
                    cancellationToken);
            }
        }
    }

    private async Task InsertCollectionItemsAsync(
        string mainTableName,
        Guid mainRecordId,
        DirectoryField field,
        object collectionData,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var collectionTableName = $"{mainTableName}_{field.ColumnName}";

        // Если collectionData - это строка с разделителями, парсим её
        var items = ParseCollectionData(collectionData);

        for (int i = 0; i < items.Count; i++)
        {
            // Определяем SQL в зависимости от типа поля
            string sql;
            if (field.DataType == FieldDataType.Reference && field.ReferenceDirectoryType != null)
            {
                sql = $@"
                INSERT INTO ""{collectionTableName}"" 
                (""IdField"", ""IdRecord"", ""{field.ReferenceDirectoryType.TableName}Id"", ""SortOrder"") 
                VALUES (@id_field, @id_record, @value, @sort_order)";
            }
            else
            {
                sql = $@"
                INSERT INTO ""{collectionTableName}"" 
                (""IdField"", ""IdRecord"", ""Value"", ""SortOrder"") 
                VALUES (@id_field, @id_record, @value, @sort_order)";
            }

            await using var command = new NpgsqlCommand(sql, connection, transaction);

            // Обязательные поля
            command.Parameters.AddWithValue("@id_field", field.Id);
            command.Parameters.AddWithValue("@id_record", mainRecordId);
            command.Parameters.AddWithValue("@value", items[i] ?? DBNull.Value);
            command.Parameters.AddWithValue("@sort_order", i);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private List<object> ParseCollectionData(object collectionData)
    {
        if (collectionData is List<object> list)
            return list;

        if (collectionData is string stringData)
        {
            return stringData.Split(',')
                .Select(item => item.Trim())
                .Where(item => !string.IsNullOrEmpty(item))
                .Cast<object>()
                .ToList();
        }

        return new List<object>();
    }
}

