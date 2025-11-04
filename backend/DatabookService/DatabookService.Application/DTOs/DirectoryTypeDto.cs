namespace DatabookService.Application.DTOs;

public record DirectoryTypeDto(
    Guid Id,
    string Name,
    string TableName,
    string? Description,
    DateTime CreatedAt,
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
    string? ReferenceDirectoryTypeName
);