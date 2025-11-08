namespace DatabookService.Application.DTOs;

public record DirectoryTypeDto(
    Guid Id,
    string Name,
    string TableName,
    string? Description,
    List<DirectoryFieldDto> Fields
);

public record DirectoryFieldDto(
    Guid Id,
    string Name,
    string ColumnName,
    int DataType,
    bool IsRequired,
    int Order,
    bool IsCollection,
    Guid? ReferenceDirectoryTypeId,
    string? ReferenceDirectoryTypeName,
    List<CollectionItemDto>? CollectionData,
    List<string>? EnumValues = null
);

public record CollectionItemDto(
    Guid Id,
    Guid DirectoryTypeItemId,
    object? Value
);