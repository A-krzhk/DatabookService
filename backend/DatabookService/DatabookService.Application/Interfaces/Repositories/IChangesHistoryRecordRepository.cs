using DatabookService.Application.DTOs.GetHistoryRecords;
using DatabookService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabookService.Application.Interfaces.Repositories
{
    public interface IChangesHistoryRecordRepository
    {
        Task AddAsync(ChangesHistoryRecord historyRecord, CancellationToken cancellationToken = default);

        Task<List<ChangesHistoryRecord>> GetByRecordIdAsync(
            Guid directoryTypeId,
            Guid recordId,
            CancellationToken cancellationToken = default);

        Task<List<ChangesHistoryRecord>> GetByDirectoryTypeAsync(
            Guid directoryTypeId,
            CancellationToken cancellationToken = default);

        Task<HistoryQueryResult> GetByDirectoryTypeWithPaginationAsync(
            Guid directoryTypeId,
            int pageNumber,
            int pageSize,
            Guid? recordId = null,
            CancellationToken cancellationToken = default);
    }
}
