namespace DatabookService.Application.DTOs.UpdateDirectoryTypes;

public record UpdateFieldDto(
    string Name,
    int Order,
    bool IsRequired
);