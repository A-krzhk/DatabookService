using DatabookService.Application.DTOs.GetHistoryRecords;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabookService.Application.Interfaces.Services
{
    public interface IChangesHistoryRecordService
    {
        Task LogRecordReadAsync(
            Guid directoryTypeId,
            Guid recordId,
            string tableName,
            Dictionary<string, object> fieldValues,
            CancellationToken cancellationToken = default);

        Task LogRecordCreationAsync(
            bool isCopy,
            Guid directoryTypeId,
            Guid recordId,
            string tableName,
            Dictionary<string, object> fieldValues,
            CancellationToken cancellationToken = default);

        Task LogRecordUpdateAsync(
            Guid directoryTypeId,
            Guid recordId,
            string tableName,
            Dictionary<string, object> oldValues,
            Dictionary<string, object> newValues,
            CancellationToken cancellationToken = default);

        Task LogRecordDeletionAsync(
            Guid directoryTypeId,
            Guid recordId,
            string tableName,
            Dictionary<string, object> oldValues,
            CancellationToken cancellationToken = default);

        Task<HistoryResponse> GetHistoryByDirectoryTypeAsync(
            Guid directoryTypeId,
            HistoryPaginationRequest request,
            CancellationToken cancellationToken = default);
    }
}
