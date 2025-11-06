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
        // Проверяем, отслеживается ли уже эта сущность
        var trackedEntity = _context.DirectoryTypes
            .Local
            .FirstOrDefault(dt => dt.Id == directoryType.Id);

        var newFields = new List<DirectoryField>();
        var hasNameOrDescriptionChanged = false;

        if (trackedEntity != null)
        {
            // Получаем ID существующих полей из базы данных
            var existingFieldIds = await _context.DirectoryFields
                .Where(f => f.DirectoryTypeId == trackedEntity.Id)
                .Select(f => f.Id)
                .ToListAsync(cancellationToken);
            
            // Обрабатываем новые DirectoryField из trackedEntity (они должны совпадать с directoryType.Fields)
            newFields = trackedEntity.Fields
                .Where(f => !existingFieldIds.Contains(f.Id))
                .ToList();

            // Добавляем новые поля в контекст
            foreach (var newField in newFields)
            {
                // Проверяем, не отслеживается ли уже это поле локально
                var trackedField = _context.DirectoryFields
                    .Local
                    .FirstOrDefault(f => f.Id == newField.Id);
                
                if (trackedField == null)
                {
                    // Добавляем новое поле в контекст
                    await _context.DirectoryFields.AddAsync(newField, cancellationToken);
                }
            }

            // Обновляем свойства сущности через метод Update только если они изменились
            hasNameOrDescriptionChanged = trackedEntity.Name != directoryType.Name || 
                                          trackedEntity.Description != directoryType.Description;
            
            if (hasNameOrDescriptionChanged)
            {
                trackedEntity.Update(directoryType.Name, directoryType.Description);
            }
            else
            {
                // Если свойства не изменились, но есть новые поля, помечаем DirectoryType как неизмененный
                // чтобы EF не пытался обновить его и не возникла ошибка оптимистичной блокировки
                var entry = _context.Entry(trackedEntity);
                if (entry.State == EntityState.Modified)
                {
                    // Помечаем все свойства как неизмененные, чтобы EF не пытался обновлять DirectoryType
                    foreach (var property in entry.Properties)
                    {
                        property.IsModified = false;
                    }
                    // Устанавливаем состояние как Unchanged, чтобы EF не пытался обновлять сущность
                    entry.State = EntityState.Unchanged;
                }
            }
        }
        else
        {
            // Если сущность не отслеживается, используем Update
            _context.DirectoryTypes.Update(directoryType);
        }

        // Сохраняем изменения (новые DirectoryField)
        await _context.SaveChangesAsync(cancellationToken);

        // Если есть новые поля и DirectoryType отслеживается, но свойства не изменились, обновляем UpdatedAt через прямой SQL
        if (trackedEntity != null && newFields.Any() && !hasNameOrDescriptionChanged)
        {
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE \"DirectoryTypes\" SET \"UpdatedAt\" = {0} WHERE \"Id\" = {1}",
                DateTime.UtcNow, trackedEntity.Id);
        }
    }

    public async Task<bool> TableNameExistsAsync(string tableName, CancellationToken cancellationToken = default)
    {
        return await _context.DirectoryTypes
            .AnyAsync(dt => dt.TableName == tableName, cancellationToken);
    }
}