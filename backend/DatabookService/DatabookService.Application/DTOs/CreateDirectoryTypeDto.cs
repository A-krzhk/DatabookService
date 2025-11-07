namespace DatabookService.Application.DTOs;

public record CreateDirectoryTypeDto(
    string Name,
    string TableName,
    string? Description,
    List<CreateDirectoryFieldDto> Fields
);

public record CreateDirectoryFieldDto(
    string Name,
    string ColumnName,
    int DataType,
    bool IsRequired,
    int Order,
    bool IsCollection,
    Guid? ReferenceDirectoryTypeId,
    List<string>? EnumValues = null
);