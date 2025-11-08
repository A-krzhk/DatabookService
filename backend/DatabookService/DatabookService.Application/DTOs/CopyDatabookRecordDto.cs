using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabookService.Application.DTOs
{
    public record CopyDatabookRecordDto(
        Guid DirectoryTypeId,
        Guid SourceRecordId
    );
}
