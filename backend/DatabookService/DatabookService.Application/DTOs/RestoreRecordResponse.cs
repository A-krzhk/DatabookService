namespace DatabookService.Application.DTOs;

public record RestoreRecordResponse(
    bool Success,
    string Message,
    Guid? RecordId = null
);