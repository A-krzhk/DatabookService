using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabookService.Application.DTOs.GetHistoryRecords
{
    public record HistoryPaginationRequest(
        int PageNumber = 1,
        int PageSize = 20,
        Guid? RecordId = null
    );
}
