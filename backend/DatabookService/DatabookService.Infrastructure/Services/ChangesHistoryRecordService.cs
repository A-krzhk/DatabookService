using DatabookService.Application.DTOs;
using DatabookService.Application.DTOs.GetDatabookRecords;
using DatabookService.Application.DTOs.GetHistoryRecords;
using DatabookService.Application.Interfaces.Repositories;
using DatabookService.Application.Interfaces.Services;
using DatabookService.Domain.Entities;
using DatabookService.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace DatabookService.Infrastructure.Services
{
    public class ChangesHistoryRecordService : IChangesHistoryRecordService
    {
        private readonly IChangesHistoryRecordRepository _historyRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public ChangesHistoryRecordService(
            IChangesHistoryRecordRepository historyRepository,
            IHttpContextAccessor httpContextAccessor)
        { 
            _historyRepository = historyRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogRecordReadAsync(
            Guid directoryTypeId,
            Guid recordId,
            string tableName,
            Dictionary<string, object> fieldValues,
            CancellationToken cancellationToken = default)
        {          
            foreach (var value in fieldValues)
            {
                string stringValue = ConvertValueToString(value.Value);

                var historyRecord = new ChangesHistoryRecord
                {
                    Id = Guid.NewGuid(),
                    Name = $"{tableName}_{recordId}_hist_read",
                    DirectoryTypeId = directoryTypeId,
                    RecordId = recordId,
                    TableName = tableName,
                    Action = ChangeAction.Read,
                    FieldName = value.Key,
                    OldValue = stringValue,
                    NewValue = null,
                    ChangedBy = GetCurrentUserName(),
                    ChangedAt = DateTime.UtcNow
                };

                await _historyRepository.AddAsync(historyRecord, cancellationToken);
            }
        }

        public async Task LogRecordCreationAsync(
            bool isCopy,
            Guid directoryTypeId,
            Guid recordId,
            string tableName,
            Dictionary<string, object> fieldValues,
            CancellationToken cancellationToken = default)
        {
            foreach (var value in fieldValues) 
            {
                string stringValue = ConvertValueToString(value.Value);

                var historyRecord = new ChangesHistoryRecord
                {
                    Id = Guid.NewGuid(),
                    Name = $"{tableName}_{recordId}_hist_{(isCopy ? "copy" : "create")}",
                    DirectoryTypeId = directoryTypeId,
                    RecordId = recordId,
                    TableName = tableName,
                    Action = isCopy ? ChangeAction.Copy : ChangeAction.Create,
                    FieldName = value.Key,
                    OldValue = null, 
                    NewValue = stringValue,
                    ChangedBy = GetCurrentUserName(),
                    ChangedAt = DateTime.UtcNow
                };

                await _historyRepository.AddAsync(historyRecord, cancellationToken);
            } 
        }

        public async Task LogRecordUpdateAsync(
            Guid directoryTypeId,
            Guid recordId,
            string tableName,
            Dictionary<string, object> oldValues,
            Dictionary<string, object> newValues,
            CancellationToken cancellationToken = default)
        {
            var changedFields = FindChangedFields(oldValues, newValues);

            foreach (var (fieldName, oldValue, newValue) in changedFields) //все изменённые значения
            {
                string stringOldValue = ConvertValueToString(oldValue);
                string stringNewValue = ConvertValueToString(newValue);

                var historyRecord = new ChangesHistoryRecord
                {
                    Id = Guid.NewGuid(),
                    Name = $"{tableName}_{recordId}_hist_update",
                    DirectoryTypeId = directoryTypeId,                 
                    RecordId = recordId,
                    TableName = tableName,
                    Action = ChangeAction.Update,
                    FieldName = fieldName,
                    OldValue = stringOldValue,
                    NewValue = stringNewValue,
                    ChangedBy = GetCurrentUserName(),
                    ChangedAt = DateTime.UtcNow
                };

                await _historyRepository.AddAsync(historyRecord, cancellationToken);
            }
        }

        public async Task LogRecordDeletionAsync(
            Guid directoryTypeId,
            Guid recordId,
            string tableName,
            Dictionary<string, object> oldValues,
            CancellationToken cancellationToken = default)
        {
            foreach (var value in oldValues)
            {
                string stringValue = ConvertValueToString(value.Value);

                var historyRecord = new ChangesHistoryRecord
                {
                    Id = Guid.NewGuid(),
                    Name = $"{tableName}_{recordId}_hist_delete",
                    DirectoryTypeId = directoryTypeId,
                    RecordId = recordId,
                    TableName = tableName,
                    Action = ChangeAction.Delete,
                    FieldName = value.Key,
                    OldValue = stringValue,
                    NewValue = null,
                    ChangedBy = GetCurrentUserName(),
                    ChangedAt = DateTime.UtcNow
                };

                await _historyRepository.AddAsync(historyRecord, cancellationToken);
            }
        }

        private string ConvertValueToString(object value)
        {
            if (value == null)
                return null;

            // Если это JSON строка
            if (value is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Array)
                {
                    var items = jsonElement.EnumerateArray()
                        .Select(element => element.ToString().Trim('"')); 
                    return string.Join(", ", items);
                }
                return jsonElement.ToString();
            }
            // Если это коллекция - сериализуем в JSON
            if (value is IEnumerable<object> enumerable && value is not string)
            {
                if (enumerable.All(x => x is string))
                {
                    return string.Join(", ", enumerable.Cast<string>());
                }

                var options = new JsonSerializerOptions
                {
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, //Разрешает кириллицу
                    WriteIndented = false
                };

                return JsonSerializer.Serialize(enumerable);
            }

            // Для простых типов используем ToString()
            return value.ToString();
        }

        private bool IsJsonArray(string value)
        {
            return value?.Trim().StartsWith("[") == true && value.Trim().EndsWith("]");
        }

        private string GetCurrentUserName()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirst(ClaimTypes.Name)?.Value
                   ?? user?.Identity?.Name
                   ?? "System";
        }

        private List<(string FieldName, object OldValue, object NewValue)> FindChangedFields(
            Dictionary<string, object> oldValues,
            Dictionary<string, object> newValues)
        {
            var changes = new List<(string, object, object)>();

            // Проверяем только измененные поля
            foreach (var (key, newValue) in newValues)
            {
                oldValues.TryGetValue(key, out var oldValue);

                if (!Equals(oldValue, newValue))
                {
                    changes.Add((key, oldValue, newValue)); //Добавляем поля, которые нужно отразить в истории
                }
            }

            // Проверяем удаленные поля (если есть в oldValues, но нет в newValues)
            foreach (var (key, oldValue) in oldValues)
            {
                if (!newValues.ContainsKey(key))
                {
                    changes.Add((key, oldValue, null));
                }
            }

            return changes;
        }

        public async Task<HistoryResponse> GetHistoryByDirectoryTypeAsync(
            Guid directoryTypeId,
            HistoryPaginationRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = await _historyRepository.GetByDirectoryTypeWithPaginationAsync(
                directoryTypeId,
                request.PageNumber,
                request.PageSize,
                request.RecordId,
                cancellationToken);

            var dtos = result.Records.Select(r => new ChangesHistoryRecordDto(
                r.Id,
                r.Name,
                r.DirectoryTypeId,
                r.RecordId,
                r.TableName,
                (int)r.Action,
                r.FieldName,
                r.OldValue,
                r.NewValue,
                r.ChangedBy,
                r.ChangedAt
            )).ToList();

            var pagination = new PaginationResponse(
                PageNumber: request.PageNumber,
                PageSize: request.PageSize,
                TotalCount: result.TotalCount,
                TotalPages: (int)Math.Ceiling(result.TotalCount / (double)request.PageSize),
                HasPrevious: request.PageNumber > 1,
                HasNext: request.PageNumber < (int)Math.Ceiling(result.TotalCount / (double)request.PageSize)
            );

            return new HistoryResponse(dtos, pagination);
        }
    }
}

