namespace DatabookService.Application.DTOs.GetDatabookRecords;

public record DatabookRecordsResponse(
    List<ColumnMetadataResponse> Columns,
    List<Dictionary<string, object>> Data,
    PaginationResponse? Pagination
);