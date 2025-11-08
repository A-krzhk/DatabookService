using DatabookService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabookService.Application.DTOs.GetHistoryRecords
{
    public class HistoryQueryResult
    {
        public List<ChangesHistoryRecord> Records { get; set; } = new();
        public int TotalCount { get; set; }
    }
}
