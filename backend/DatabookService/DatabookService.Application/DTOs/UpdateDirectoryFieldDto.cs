namespace DatabookService.Application.DTOs;

public record UpdateDirectoryFieldDto(
    Guid? Id, // null для новых полей
    string Name,
    string? ColumnName, // только для новых полей
    int? DataType, // только для новых полей
    bool? IsRequired, // только для новых полей
    int Order,
    bool? IsCollection, // только для новых полей
    Guid? ReferenceDirectoryTypeId // только для новых полей
);
