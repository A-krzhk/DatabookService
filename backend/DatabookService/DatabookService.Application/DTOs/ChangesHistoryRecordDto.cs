using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabookService.Application.DTOs
{
    public record ChangesHistoryRecordDto(
        Guid Id,
        string Name,
        Guid DirectoryTypeId,
        Guid RecordId,
        string TableName,
        int Action,
        string FieldName,
        string OldValue,
        string NewValue,
        string ChangedBy,
        DateTime ChangedAt
    );
}
