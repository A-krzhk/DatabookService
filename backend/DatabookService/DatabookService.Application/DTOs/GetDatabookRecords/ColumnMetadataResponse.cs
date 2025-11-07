namespace DatabookService.Application.DTOs.GetDatabookRecords;

public record ColumnMetadataResponse(
    string FieldName,
    string DisplayName,
    string DataType,
    bool IsCollection,
    bool IsRequired,
    int? MaxLength,
    ReferenceInfoResponse? Reference
);