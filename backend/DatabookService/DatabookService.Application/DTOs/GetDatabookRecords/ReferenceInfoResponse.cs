namespace DatabookService.Application.DTOs.GetDatabookRecords;

public record ReferenceInfoResponse(
    Guid? DirectoryTypeId,
    string? TableName
);