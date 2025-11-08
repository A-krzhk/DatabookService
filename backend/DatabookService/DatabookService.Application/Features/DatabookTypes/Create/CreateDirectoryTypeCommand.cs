using DatabookService.Application.DTOs;
using DatabookService.Application.Interfaces;
using DatabookService.Application.Interfaces.Services;
using DatabookService.Domain.Entities;
using DatabookService.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DatabookService.Application.Features.DatabookTypes.Create;

public class CreateDirectoryTypeCommand
{
    private readonly IDirectoryTypeRepository _repository;
    private readonly IDynamicTableService _dynamicTableService;
    private readonly ILogger<CreateDirectoryTypeCommand> _logger;

    public CreateDirectoryTypeCommand(
        IDirectoryTypeRepository repository,
        IDynamicTableService dynamicTableService,
        ILogger<CreateDirectoryTypeCommand> logger)
    {
        _repository = repository;
        _dynamicTableService = dynamicTableService;
        _logger = logger;
    }

    public async Task<DirectoryTypeDto> ExecuteAsync(
        CreateDirectoryTypeDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating Databook Directory Type");
        // РџСЂРѕРІРµСЂРєР° СЃСѓС‰РµСЃС‚РІРѕРІР°РЅРёСЏ С‚Р°Р±Р»РёС†С‹
        if (await _repository.TableNameExistsAsync(dto.TableName, cancellationToken))
        {
            throw new InvalidOperationException($"Directory type with table name '{dto.TableName}' already exists");
        }

        // РЎРѕР·РґР°РЅРёРµ РґРѕРјРµРЅРЅРѕР№ СЃСѓС‰РЅРѕСЃС‚Рё
        var directoryType = new DirectoryType(dto.Name, dto.TableName, dto.Description);

        // Р”РѕР±Р°РІР»РµРЅРёРµ РїРѕР»РµР№
        foreach (var fieldDto in dto.Fields.OrderBy(f => f.Order))
        {
            // Р’Р°Р»РёРґР°С†РёСЏ РґР»СЏ Enum РїРѕР»РµР№
            if ((FieldDataType)fieldDto.DataType == FieldDataType.Enum)
            {
                // РџСЂРѕРІРµСЂРєР° С‡С‚Рѕ РїРµСЂРµРґР°РЅС‹ Р·РЅР°С‡РµРЅРёСЏ Enum
                if (fieldDto.EnumValues == null || !fieldDto.EnumValues.Any())
                {
                    throw new InvalidOperationException(
                        $"Enum field '{fieldDto.Name}' must have at least one value in EnumValues");
                }

                // РџСЂРѕРІРµСЂРєР° С‡С‚Рѕ РІСЃРµ Р·РЅР°С‡РµРЅРёСЏ РЅРµ РїСѓСЃС‚С‹Рµ
                if (fieldDto.EnumValues.Any(string.IsNullOrWhiteSpace))
                {
                    throw new InvalidOperationException(
                        $"Enum field '{fieldDto.Name}' cannot have empty values in EnumValues");
                }

                // РџСЂРѕРІРµСЂРєР° РЅР° РґСѓР±Р»РёРєР°С‚С‹ (РѕРїС†РёРѕРЅР°Р»СЊРЅРѕ)
                if (fieldDto.EnumValues.Distinct().Count() != fieldDto.EnumValues.Count)
                {
                    throw new InvalidOperationException(
                        $"Enum field '{fieldDto.Name}' has duplicate values in EnumValues");
                }
            }
            else
            {
                // Р”Р»СЏ РЅРµ-Enum РїРѕР»РµР№ EnumValues РґРѕР»Р¶РµРЅ Р±С‹С‚СЊ null РёР»Рё РїСѓСЃС‚С‹Рј
                if (fieldDto.EnumValues != null && fieldDto.EnumValues.Any())
                {
                    throw new InvalidOperationException(
                        $"Non-enum field '{fieldDto.Name}' cannot have EnumValues");
                }
            }

            // РџСЂРѕРІРµСЂРєР° СЃСѓС‰РµСЃС‚РІРѕРІР°РЅРёСЏ СЃСЃС‹Р»РѕС‡РЅРѕРіРѕ СЃРїСЂР°РІРѕС‡РЅРёРєР°
            if (fieldDto.DataType == (int)FieldDataType.Reference && fieldDto.ReferenceDirectoryTypeId.HasValue)
            {
                var referenceType = await _repository.GetByIdAsync(
                    fieldDto.ReferenceDirectoryTypeId.Value, 
                    cancellationToken);
                
                if (referenceType == null)
                {
                    throw new InvalidOperationException(
                        $"Reference directory type with ID '{fieldDto.ReferenceDirectoryTypeId}' not found");
                }
            }



            var field = new DirectoryField(
                directoryType.Id,
                fieldDto.Name,
                fieldDto.ColumnName,
                (FieldDataType)fieldDto.DataType,
                fieldDto.IsRequired,
                fieldDto.Order,
                fieldDto.IsCollection,
                fieldDto.ReferenceDirectoryTypeId,
                fieldDto.EnumValues
            );

            directoryType.AddField(field);
        }

        // РЎРѕС…СЂР°РЅРµРЅРёРµ РІ Р‘Р”
        var savedDirectoryType = await _repository.AddAsync(directoryType, cancellationToken);

        // РЎРѕР·РґР°РЅРёРµ С„РёР·РёС‡РµСЃРєРѕР№ С‚Р°Р±Р»РёС†С‹ РІ Р‘Р”
        await _dynamicTableService.CreateTableAsync(savedDirectoryType, cancellationToken);

        // РњР°РїРїРёРЅРі РІ DTO
        return MapToDto(savedDirectoryType);
    }

    private DirectoryTypeDto MapToDto(DirectoryType directoryType)
    {
        return new DirectoryTypeDto(
            directoryType.Id,
            directoryType.Name,
            directoryType.TableName,
            directoryType.Description,
            directoryType.Fields.Select(f => new DirectoryFieldDto(
                f.Id,
                f.Name,
                f.ColumnName,
                (int)f.DataType,
                f.IsRequired,
                f.Order,
                f.IsCollection,
                f.ReferenceDirectoryTypeId,
                f.ReferenceDirectoryType?.Name,
                f.EnumValues,
                null // CollectionData РЅРµ Р·Р°РіСЂСѓР¶Р°РµС‚СЃСЏ РїСЂРё СЃРѕР·РґР°РЅРёРё
            )).ToList()
        );
    }
}

