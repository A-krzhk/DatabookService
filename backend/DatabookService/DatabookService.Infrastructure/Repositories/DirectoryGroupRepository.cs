using DatabookService.Application.Interfaces.Repositories;
using DatabookService.Domain.Entities;
using DatabookService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DatabookService.Infrastructure.Repositories;

public class DirectoryGroupRepository : IDirectoryGroupRepository
{
    private readonly ApplicationDbContext _context;

    public DirectoryGroupRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<DirectoryGroup>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DirectoryGroups
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<DirectoryGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.DirectoryGroups.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<DirectoryGroup> AddAsync(DirectoryGroup group, CancellationToken cancellationToken = default)
    {
        await _context.DirectoryGroups.AddAsync(group, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return group;
    }

    public async Task UpdateAsync(DirectoryGroup group, CancellationToken cancellationToken = default)
    {
        _context.DirectoryGroups.Update(group);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var group = await _context.DirectoryGroups.FindAsync(new object[] { id }, cancellationToken);
        if (group == null) return;
        _context.DirectoryGroups.Remove(group);
        await _context.SaveChangesAsync(cancellationToken);
    }
}