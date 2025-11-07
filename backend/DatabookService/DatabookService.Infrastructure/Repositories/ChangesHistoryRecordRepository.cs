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

        public async Task<List<ChangesHistoryRecord>> GetByDirectoryTypeWithPaginationAsync(
            Guid directoryTypeId,
            int page = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            return await _context.ChangesHistoryRecords.Where(chr => chr.DirectoryTypeId == directoryTypeId)
                                .OrderByDescending(chr => chr.ChangedAt)
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .AsNoTracking()
                                .ToListAsync(cancellationToken);
        }
    }
}
