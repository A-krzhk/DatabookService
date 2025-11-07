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

    }
}
