namespace DatabookService.Application.DTOs.GetDatabookRecords;

public record PaginationResponse(
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPrevious,
    bool HasNext
);