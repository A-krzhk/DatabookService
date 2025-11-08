using System.Collections.Generic;

namespace DatabookService.Application.DTOs;

public class UpdateDatabookRecordDto
{
    public Dictionary<string, object> FieldsValues { get; set; } = new();
}
