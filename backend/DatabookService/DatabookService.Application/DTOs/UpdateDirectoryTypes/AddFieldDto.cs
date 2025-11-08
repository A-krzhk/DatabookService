using System.Collections.Generic;

namespace DatabookService.Application.DTOs.UpdateDirectoryTypes;

public record AddFieldDto(
    string Name,
    string ColumnName,
    int DataType,
    bool IsRequired,
    int Order,
    bool IsCollection,
    Guid? ReferenceDirectoryTypeId = null,
    List<string>? EnumValues = null
);
