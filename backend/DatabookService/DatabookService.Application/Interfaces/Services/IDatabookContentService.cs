using DatabookService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabookService.Application.Interfaces.Services
{
    public interface IDatabookContentService
    {
        Task<Guid?> InsertValues(
            DirectoryType tableName,
            IReadOnlyCollection<DirectoryField> expectedFields,
            Dictionary<string, object> actualFields,
            CancellationToken cancellationToken = default);
        
        Task UpdateValues(
            DirectoryType tableName,
            IReadOnlyCollection<DirectoryField> expectedFields,
            Guid recordId,
            Dictionary<string, object> actualFields,
            CancellationToken cancellationToken = default);
        
        Task<List<Dictionary<string, object>>> GetAllRecordsAsync(
            string tableName,
            IReadOnlyCollection<DirectoryField> fields,
            int? pageNumber = null,
            int? pageSize = null,
            CancellationToken cancellationToken = default);
        
        Task<Dictionary<string, object>?> GetRecordByIdAsync(
            string tableName, 
            Guid id, 
            IReadOnlyCollection<DirectoryField> fields, 
            CancellationToken cancellationToken = default);
        
        Task<int> GetTotalCountAsync(string tableName, CancellationToken cancellationToken = default);
        
        Task<List<Dictionary<string, object>>> GetAllDeletedRecordsAsync(
            string tableName,
            IReadOnlyCollection<DirectoryField> fields,
            int? pageNumber = null,
            int? pageSize = null,
            CancellationToken cancellationToken = default);

        Task<int> GetTotalDeletedCountAsync(
            string tableName,
            CancellationToken cancellationToken = default);

        Task<bool> RestoreRecordAsync(
            string tableName,
            Guid recordId,
            CancellationToken cancellationToken = default);

        Task<Guid> InsertCopiedRecordAsync(
            DirectoryType directoryType,
            Dictionary<string, object> sourceRecord,
            CancellationToken cancellationToken = default);
    }
}
