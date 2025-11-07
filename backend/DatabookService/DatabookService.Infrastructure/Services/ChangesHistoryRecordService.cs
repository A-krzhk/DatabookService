using DatabookService.Application.Interfaces.Repositories;
using DatabookService.Application.Interfaces.Services;
using DatabookService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabookService.Infrastructure.Services
{
    public class ChangesHistoryRecordService : IChangesHistoryRecordService
    {
        private readonly IChangesHistoryRecordRepository _historyRepository;
        public ChangesHistoryRecordService(IChangesHistoryRecordRepository historyRepository)
        { 
            _historyRepository = historyRepository;
        }

        public async Task LogRecordCreationAsync(
            Guid directoryTypeId,
            Guid recordId,
            string tableName,
            Dictionary<string, object> fieldValues,
            CancellationToken cancellationToken = default)
        {
            var historyRecord = new ChangesHistoryRecord
            {
                Id = Guid.NewGuid(),
                DirectoryTypeId = directoryTypeId,
                RecordId = recordId,
                TableName = tableName,
                Action = AuditAction.Create,
                FieldName = null,
                OldValue = null,
                NewValue = JsonSerializer.Serialize(fieldValues),
                ChangedBy = _currentUserService.UserId,
                ChangedAt = DateTime.UtcNow
            };

            await _historyRepository.AddAsync(historyRecord, cancellationToken);
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

            foreach (var (fieldName, oldValue, newValue) in changedFields)
            {
                var historyRecord = new ChangesHistoryRecord
                {
                    Id = Guid.NewGuid(),
                    DirectoryTypeId = directoryTypeId,
                    RecordId = recordId,
                    TableName = tableName,
                    Action = AuditAction.Update,
                    FieldName = fieldName,
                    OldValue = oldValue?.ToString(),
                    NewValue = newValue?.ToString(),
                    ChangedBy = _currentUserService.UserId,
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
            var historyRecord = new ChangesHistoryRecord
            {
                Id = Guid.NewGuid(),
                DirectoryTypeId = directoryTypeId,
                RecordId = recordId,
                TableName = tableName,
                Action = AuditAction.Delete,
                FieldName = null,
                OldValue = JsonSerializer.Serialize(oldValues),
                NewValue = null,
                ChangedBy = _currentUserService.UserId,
                ChangedAt = DateTime.UtcNow
            };

            await _historyRepository.AddAsync(historyRecord, cancellationToken);
        }

        private List<(string FieldName, object OldValue, object NewValue)> FindChangedFields(
            Dictionary<string, object> oldValues,
            Dictionary<string, object> newValues)
        {
            var changes = new List<(string, object, object)>();

            // Проверяем измененные поля
            foreach (var (key, newValue) in newValues)
            {
                oldValues.TryGetValue(key, out var oldValue);

                if (!Equals(oldValue, newValue))
                {
                    changes.Add((key, oldValue, newValue));
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

