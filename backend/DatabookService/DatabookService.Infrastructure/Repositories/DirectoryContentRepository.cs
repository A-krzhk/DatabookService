using DatabookService.Application.Interfaces.Repositories;
using DatabookService.Domain.Entities;
using DatabookService.Domain.Enums;
using DatabookService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace DatabookService.Infrastructure.Repositories;

public class DirectoryContentRepository : IDirectoryContentRepository
{
    private readonly ApplicationDbContext _context;
    private readonly string _connectionString;

    public DirectoryContentRepository(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }
    
    /// Помечает запись как удалённую (IsDeleted = true, DeletedAt = now)
    public async Task<bool> MarkAsDeletedAsync(Guid directoryTypeId, Guid recordId, CancellationToken cancellationToken)
    {
        var directoryType = await _context.DirectoryTypes
            .Include(dt => dt.Fields)
            .FirstOrDefaultAsync(dt => dt.Id == directoryTypeId, cancellationToken);

        if (directoryType == null)
            return false;

        var tableName = directoryType.TableName;
        var sql = $@"
            UPDATE ""{tableName}""
            SET ""IsDeleted"" = TRUE,
                ""DeletedDate"" = NOW()
            WHERE ""Id"" = @id";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("@deletedAt", DateTime.UtcNow);
        command.Parameters.AddWithValue("@id", recordId);

        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        return rows > 0;
    }

    /// Возвращает записи, удалённые более age дней назад
    public async Task<List<(Guid DirectoryTypeId, Guid RecordId, string TableName)>> GetSoftDeletedOlderThanAsync(TimeSpan age, CancellationToken cancellationToken)
    {
        var threshold = DateTime.UtcNow - age;
        var result = new List<(Guid, Guid, string)>();

        var directoryTypes = await _context.DirectoryTypes.ToListAsync(cancellationToken);

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        foreach (var dt in directoryTypes)
        {
            var sql = $@"SELECT ""Id"" FROM ""{dt.TableName}""
                         WHERE ""IsDeleted"" = TRUE AND ""DeletedAt"" < @threshold";

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@threshold", threshold);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var recordId = reader.GetGuid(0);
                result.Add((dt.Id, recordId, dt.TableName));
            }
            await reader.CloseAsync();
        }

        return result;
    }
    
    /// Удаляет запись физически, включая коллекционные таблицы
    public async Task DeletePhysicallyAsync(Guid directoryTypeId, Guid recordId, string tableName, CancellationToken cancellationToken)
    {
        var directoryType = await _context.DirectoryTypes
            .Include(dt => dt.Fields)
            .FirstOrDefaultAsync(dt => dt.Id == directoryTypeId, cancellationToken);

        if (directoryType == null)
            return;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            // Удаляем связанные записи из коллекций
            var collectionFields = directoryType.Fields.Where(f => f.IsCollection);
            foreach (var field in collectionFields)
            {
                var collectionTable = $"{tableName}_{field.ColumnName}";
                var sqlDeleteCollection = $@"DELETE FROM ""{collectionTable}"" WHERE ""IdRecord"" = @recordId";
                await using var command = new NpgsqlCommand(sqlDeleteCollection, connection, transaction);
                command.Parameters.AddWithValue("@recordId", recordId);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            // Удаляем саму запись
            var sqlDeleteMain = $@"DELETE FROM ""{tableName}"" WHERE ""Id"" = @recordId";
            await using (var command = new NpgsqlCommand(sqlDeleteMain, connection, transaction))
            {
                command.Parameters.AddWithValue("@recordId", recordId);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<List<Dictionary<string, object>>> GetAllRecordsAsync(
        string tableName,
        IReadOnlyCollection<DirectoryField> fields,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Основная таблица
        var regularFields = fields.Where(f => !f.IsCollection).ToList();
        var columnNames = string.Join(", ", regularFields.Select(f => $"\"{f.ColumnName}\""));
        var sql = $"SELECT \"Id\", {columnNames} FROM \"{tableName}\" WHERE \"IsDeleted\" = FALSE";

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

                // Для ссылок и простых коллекций разная логика
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

        // Основные поля
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

        // Подгружаем коллекционные поля
        var collectionFields = fields.Where(f => f.IsCollection).ToList();
        foreach (var field in collectionFields)
        {
            var collectionTableName = $"{tableName}_{field.ColumnName}";
            var sqlCollection = "";

            if (field.DataType == FieldDataType.Reference)
            {
                sqlCollection = $@"SELECT ""{field.ReferenceDirectoryType.TableName}Id"" FROM ""{collectionTableName}""
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

            result[field.ColumnName] = values;
        }

        return result;
    }
}