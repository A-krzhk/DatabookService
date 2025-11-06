using DatabookService.Application.Interfaces.Services;
using DatabookService.Domain.Entities;
using DatabookService.Domain.Enums;
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
    public class DatabookContentService : IDatabookContentService
    {
        private readonly string _connectionString;
        private readonly ITypesValidationService _typesValidationService;

        public DatabookContentService(IConfiguration configuration, ITypesValidationService typesValidationService) 
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _typesValidationService = typesValidationService;
        }

        public async Task<int> InsertValues(
            string tableName,
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
                var recordId = new Guid();
                recordId = (Guid)await InsertMainRecord(tableName, expectedFields, actualFields, connection, transaction, cancellationToken);

                // Вставляем коллекции таблицу коллекции
                var collectionFields = expectedFields
                                        .Where(f => f.IsCollection && actualFields.ContainsKey(f.ColumnName));

                //Если полей-коллекций нет, то и вставляться ничего не будет
                foreach (var field in collectionFields)
                {
                    var values = actualFields[field.ColumnName];
                    await InsertCollectionItems(tableName, (Guid)recordId, field, values, connection, transaction, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                return 1;
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
            // Только не коллекционные поля
            var regularFields = expectedFields
                .Where(f => !f.IsCollection && actualFields.ContainsKey(f.ColumnName))
                .ToList();
            string sql = "";
            object result;

            if (regularFields.Any())
            {
                var columnNames = string.Join(", ", regularFields.Select(f => $@"""{f.ColumnName}"""));
                var parameterNames = string.Join(", ", regularFields.Select(f => $"@p_{f.ColumnName}"));

                sql = $@"INSERT INTO ""{tableName}"" ({columnNames}) VALUES ({parameterNames}) RETURNING ""Id""";

                // Параметры запроса
                var parameters = regularFields.Select(f =>
                {
                    var value = actualFields[f.ColumnName] ?? DBNull.Value;
                    value = _typesValidationService.ConvertJsonToCorrectType(f, value);
                    return new NpgsqlParameter($"@p_{f.ColumnName}", value);
                }).ToList();

                await using var command = new NpgsqlCommand(sql, connection, transaction);
                command.Parameters.AddRange(parameters.ToArray());
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
                foreach (var (item, index) in array.Select((item, index) => (item, index)))
                {
                    await InsertCollectionItem(collectionTableName, mainRecordId, field, item, index, connection, transaction, cancellationToken);
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
            var valueTableName = "Value";
            if (field.DataType == FieldDataType.Reference)
            {
                valueTableName = $"{field.ReferenceDirectoryType.TableName}Id";
            }

            var sql = $@"INSERT INTO ""{collectionTableName}"" (""IdField"", ""IdRecord"", ""{valueTableName}"", ""SortOrder"")
            VALUES (@id_field, @id_record, @value, @sort_order)";

            // Конвертируем значение в строку для универсального хранения
            var stringValue = _typesValidationService.ConvertJsonToCorrectType(field, value);

            await using var command = new NpgsqlCommand(sql, connection, transaction);

            command.Parameters.AddWithValue("@id_field", field.Id);
            command.Parameters.AddWithValue("@id_record", mainRecordId);
            command.Parameters.AddWithValue("@value", stringValue ?? DBNull.Value);
            command.Parameters.AddWithValue("@sort_order", sortOrder);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
