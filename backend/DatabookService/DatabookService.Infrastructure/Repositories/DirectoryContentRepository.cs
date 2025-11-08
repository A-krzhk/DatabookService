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
    
}