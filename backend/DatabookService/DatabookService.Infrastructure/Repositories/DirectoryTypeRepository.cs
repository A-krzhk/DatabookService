using DatabookService.Application.Interfaces;
using DatabookService.Domain.Entities;
using DatabookService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DatabookService.Infrastructure.Repositories;

public class DirectoryTypeRepository : IDirectoryTypeRepository
{
    private readonly ApplicationDbContext _context;

    public DirectoryTypeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DirectoryType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.DirectoryTypes
            .Include(dt => dt.Fields)
                .ThenInclude(f => f.ReferenceDirectoryType)
            .FirstOrDefaultAsync(dt => dt.Id == id, cancellationToken);
    }

    public async Task<DirectoryType?> GetByTableNameAsync(string tableName, CancellationToken cancellationToken = default)
    {
        return await _context.DirectoryTypes
            .Include(dt => dt.Fields)
                .ThenInclude(f => f.ReferenceDirectoryType)
            .FirstOrDefaultAsync(dt => dt.TableName == tableName, cancellationToken);
    }

    public async Task<List<DirectoryType>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DirectoryTypes
            .Include(dt => dt.Fields)
                .ThenInclude(f => f.ReferenceDirectoryType)
            .OrderBy(dt => dt.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<DirectoryType> AddAsync(DirectoryType directoryType, CancellationToken cancellationToken = default)
    {
        await _context.DirectoryTypes.AddAsync(directoryType, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return directoryType;
    }

    public async Task UpdateAsync(DirectoryType directoryType, CancellationToken cancellationToken = default)
    {
        _context.DirectoryTypes.Update(directoryType);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TableNameExistsAsync(string tableName, CancellationToken cancellationToken = default)
    {
        return await _context.DirectoryTypes
            .AnyAsync(dt => dt.TableName == tableName, cancellationToken);
    }
}