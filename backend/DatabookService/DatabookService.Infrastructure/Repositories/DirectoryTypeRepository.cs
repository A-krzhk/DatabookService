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
    
    public async Task<DirectoryField?> GetFieldByIdAsync(Guid fieldId, CancellationToken cancellationToken = default)
    {
        return await _context.DirectoryFields
            .Include(f => f.ReferenceDirectoryType)
            .Include(f => f.DirectoryType)
            .FirstOrDefaultAsync(f => f.Id == fieldId, cancellationToken);
    }
    
    public async Task<DirectoryField> AddFieldAsync(Guid directoryTypeId, DirectoryField field, CancellationToken cancellationToken = default)
    {
        await _context.DirectoryFields.AddAsync(field, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    
        // Обновляем UpdatedAt у DirectoryType
        var directoryType = await _context.DirectoryTypes.FindAsync(new object[] { directoryTypeId }, cancellationToken);
        if (directoryType != null)
        {
            directoryType.Update(directoryType.Name, directoryType.Description);
            await _context.SaveChangesAsync(cancellationToken);
        }
    
        return field;
    }
    
    public async Task RemoveFieldAsync(Guid fieldId, CancellationToken cancellationToken = default)
    {
        var field = await _context.DirectoryFields.FindAsync(new object[] { fieldId }, cancellationToken);
        if (field == null)
        {
            throw new InvalidOperationException($"Field with ID '{fieldId}' not found");
        }
    
        var directoryTypeId = field.DirectoryTypeId;
    
        _context.DirectoryFields.Remove(field);
        await _context.SaveChangesAsync(cancellationToken);
    
        // Обновляем UpdatedAt у DirectoryType
        var directoryType = await _context.DirectoryTypes.FindAsync(new object[] { directoryTypeId }, cancellationToken);
        if (directoryType != null)
        {
            directoryType.Update(directoryType.Name, directoryType.Description);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
    
    public async Task UpdateFieldAsync(DirectoryField field, CancellationToken cancellationToken = default)
    {
        _context.DirectoryFields.Update(field);
        await _context.SaveChangesAsync(cancellationToken);
    
        // Обновляем UpdatedAt у DirectoryType
        var directoryType = await _context.DirectoryTypes.FindAsync(new object[] { field.DirectoryTypeId }, cancellationToken);
        if (directoryType != null)
        {
            directoryType.Update(directoryType.Name, directoryType.Description);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}