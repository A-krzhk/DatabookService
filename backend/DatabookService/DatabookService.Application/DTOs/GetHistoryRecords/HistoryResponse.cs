using DatabookService.Application.DTOs.GetDatabookRecords;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabookService.Application.DTOs.GetHistoryRecords
{
    public record HistoryResponse(
        List<ChangesHistoryRecordDto> Records,
        PaginationResponse Pagination
    );
}
