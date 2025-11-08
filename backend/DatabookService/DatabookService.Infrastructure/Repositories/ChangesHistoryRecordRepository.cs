using DatabookService.Application.DTOs.GetHistoryRecords;
using DatabookService.Application.Interfaces.Repositories;
using DatabookService.Domain.Entities;
using DatabookService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabookService.Infrastructure.Repositories
{
    public class ChangesHistoryRecordRepository : IChangesHistoryRecordRepository
    {
        private readonly ApplicationDbContext _context;

        public ChangesHistoryRecordRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(ChangesHistoryRecord historyRecord, CancellationToken cancellationToken = default)
        {
            await _context.ChangesHistoryRecords.AddAsync(historyRecord, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<ChangesHistoryRecord>> GetByRecordIdAsync(
            Guid directoryTypeId,
            Guid recordId,
            CancellationToken cancellationToken = default)
        {
            return await _context.ChangesHistoryRecords.Where(chr => chr.DirectoryTypeId == directoryTypeId && chr.RecordId == recordId)
                .OrderByDescending(chr => chr.ChangedAt) 
                .Include(chr => chr.DirectoryType) 
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<List<ChangesHistoryRecord>> GetByDirectoryTypeAsync(
            Guid directoryTypeId,
            CancellationToken cancellationToken = default)
        {
            return await _context.ChangesHistoryRecords.Where(chr => chr.DirectoryTypeId == directoryTypeId)
                            .OrderByDescending(chr => chr.ChangedAt)
                            .AsNoTracking()
                            .ToListAsync(cancellationToken);
        }

        public async Task<HistoryQueryResult> GetByDirectoryTypeWithPaginationAsync(
            Guid directoryTypeId,
            int pageNumber,
            int pageSize,
            Guid? recordId = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.ChangesHistoryRecords
                .Where(chr => chr.DirectoryTypeId == directoryTypeId);

            if (recordId.HasValue)
            {
                query = query.Where(chr => chr.RecordId == recordId.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var records = await query
                .OrderByDescending(chr => chr.ChangedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return new HistoryQueryResult
            {
                Records = records,
                TotalCount = totalCount
            };
        }
    }
}
