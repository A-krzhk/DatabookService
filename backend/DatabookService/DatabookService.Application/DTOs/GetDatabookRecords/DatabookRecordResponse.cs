namespace DatabookService.Application.DTOs.GetDatabookRecords;

public record DatabookRecordResponse(
    Dictionary<string, object> Data,
    List<ColumnMetadataResponse> Columns
);