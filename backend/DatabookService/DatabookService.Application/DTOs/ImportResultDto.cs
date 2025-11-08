using System.Collections.Generic;

namespace DatabookService.Application.DTOs;

public record ImportResultDto(
    int Imported,
    int Skipped,
    List<string> Errors
);
