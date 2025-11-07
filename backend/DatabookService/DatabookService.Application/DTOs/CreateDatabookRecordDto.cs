using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabookService.Application.DTOs
{
    public class CreateDatabookRecordDto
    {
        public Guid TableId { get; set; }
        public Dictionary<string, object> FieldsValues { get; set; } 
    }
}
