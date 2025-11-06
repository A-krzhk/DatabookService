using DatabookService.Application.DTOs;
using DatabookService.Application.Interfaces.Repositories;
using DatabookService.Domain.Enums;
using DatabookService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DatabookService.Infrastructure.Repositories;

public class DirectoryCollectionRepository : IDirectoryCollectionRepository
{
    private readonly ApplicationDbContext _context;

    public DirectoryCollectionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CollectionItemDto>> GetCollectionDataAsync(
        string tableName,
        string columnName,
        FieldDataType dataType,
        string? referenceTableName,
        CancellationToken cancellationToken = default)
    {
        var collectionTableName = $"{tableName}_{columnName}";
        var directoryTypeIdColumn = $"{tableName}Id";

        try
        {
            // Проверяем существование таблицы
            if (!await TableExistsAsync(collectionTableName, cancellationToken))
            {
                return new List<CollectionItemDto>();
            }

            var collectionData = new List<CollectionItemDto>();
            var connection = _context.Database.GetDbConnection();

            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            try
            {
                if (dataType == FieldDataType.Reference && !string.IsNullOrEmpty(referenceTableName))
                {
                    // Для ссылочных коллекций: загружаем Id и ReferenceDirectoryTypeId
                    var referenceIdColumn = $"{referenceTableName}Id";
                    var sql = $@"
                        SELECT ""Id"", ""{directoryTypeIdColumn}"", ""{referenceIdColumn}""
                        FROM ""{collectionTableName}""
                        ORDER BY ""Id""";

                    using var command = connection.CreateCommand();
                    command.CommandText = sql;

                    using var reader = await command.ExecuteReaderAsync(cancellationToken);
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var id = reader.GetGuid(0);
                        var directoryTypeItemId = reader.GetGuid(1);
                        var referenceId = reader.GetGuid(2);

                        collectionData.Add(new CollectionItemDto(
                            id,
                            directoryTypeItemId,
                            referenceId
                        ));
                    }
                }
                else
                {
                    // Для простых типов: загружаем Id, DirectoryTypeItemId и Value
                    var sql = $@"
                        SELECT ""Id"", ""{directoryTypeIdColumn}"", ""Value""
                        FROM ""{collectionTableName}""
                        ORDER BY ""Id""";

                    using var command = connection.CreateCommand();
                    command.CommandText = sql;

                    using var reader = await command.ExecuteReaderAsync(cancellationToken);
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var id = reader.GetGuid(0);
                        var directoryTypeItemId = reader.GetGuid(1);
                        object? value = reader.IsDBNull(2) ? null : reader.GetValue(2);

                        collectionData.Add(new CollectionItemDto(
                            id,
                            directoryTypeItemId,
                            value
                        ));
                    }
                }
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
            }

            return collectionData;
        }
        catch
        {
            // Если таблица не существует или произошла ошибка, возвращаем пустой список
            return new List<CollectionItemDto>();
        }
    }

    private async Task<bool> TableExistsAsync(string tableName, CancellationToken cancellationToken)
    {
        try
        {
            var connection = _context.Database.GetDbConnection();
            
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync(cancellationToken);
            }

            try
            {
                var sql = @"
                    SELECT EXISTS (
                        SELECT FROM information_schema.tables 
                        WHERE table_schema = 'public' 
                        AND table_name = @tableName
                    )";

                using var command = connection.CreateCommand();
                command.CommandText = sql;
                
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@tableName";
                parameter.Value = tableName;
                command.Parameters.Add(parameter);

                var result = await command.ExecuteScalarAsync(cancellationToken);
                return result is bool exists && exists;
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
            }
        }
        catch
        {
            return false;
        }
    }
}


