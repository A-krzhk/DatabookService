using DatabookService.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabookService.Domain.Entities
{
    public class ChangesHistoryRecord
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid DirectoryTypeId { get; set; }    
        public Guid RecordId { get; set; }            
        public string TableName { get; set; }         
        public ChangeAction Action { get; set; }       
        public string? FieldName { get; set; }        
        public string? OldValue { get; set; }        
        public string? NewValue { get; set; }     
        public string ChangedBy { get; set; }       
        public DateTime ChangedAt { get; set; }

        public DirectoryType DirectoryType { get; set; }
    }

    
}
