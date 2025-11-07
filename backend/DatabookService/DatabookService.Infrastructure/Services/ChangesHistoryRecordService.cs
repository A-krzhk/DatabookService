using DatabookService.Application.Interfaces.Repositories;
using DatabookService.Application.Interfaces.Services;
using DatabookService.Domain.Entities;
using DatabookService.Domain.Enums;
using Microsoft.AspNetCore.Http;
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

        public async Task LogRecordCreationAsync(
            Guid directoryTypeId,
            Guid recordId,
            string tableName,
            Dictionary<string, object> fieldValues,
            CancellationToken cancellationToken = default)
        {
            foreach (var value in fieldValues) 
            {
                var historyRecord = new ChangesHistoryRecord
                {
                    Id = Guid.NewGuid(),
                    Name = $"{tableName}_{recordId}_hist_create",
                    DirectoryTypeId = directoryTypeId,
                    RecordId = recordId,
                    TableName = tableName,
                    Action = ChangeAction.Create,
                    FieldName = value.Key,
                    OldValue = null, 
                    NewValue = value.Value?.ToString(),
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
                var historyRecord = new ChangesHistoryRecord
                {
                    Id = Guid.NewGuid(),
                    Name = $"{tableName}_{recordId}_hist_update",
                    DirectoryTypeId = directoryTypeId,                 
                    RecordId = recordId,
                    TableName = tableName,
                    Action = ChangeAction.Update,
                    FieldName = fieldName,
                    OldValue = oldValue?.ToString(),
                    NewValue = newValue?.ToString(),
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
                var historyRecord = new ChangesHistoryRecord
                {
                    Id = Guid.NewGuid(),
                    Name = $"{tableName}_{recordId}_hist_delete",
                    DirectoryTypeId = directoryTypeId,
                    RecordId = recordId,
                    TableName = tableName,
                    Action = ChangeAction.Delete,
                    FieldName = value.Key,
                    OldValue = value.Value?.ToString(),
                    NewValue = null,
                    ChangedBy = GetCurrentUserName(),
                    ChangedAt = DateTime.UtcNow
                };

                await _historyRepository.AddAsync(historyRecord, cancellationToken);
            }
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
    }
}

