using DatabookService.Application.DTOs;
using DatabookService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabookService.Application.Interfaces.Services
{
    public interface ITypesValidationService
    {
        object ConvertJsonToCorrectType(DirectoryField f, object value);
        Task<bool> IsReferenceCorrect(
            string tableName,
            object value,
            CancellationToken cancellationToken = default);
        Task<ValidationResult> ValidateFields(
            string tableName,
            IReadOnlyCollection<DirectoryField> expectedFields,
            Dictionary<string, object> actualFields,
            CancellationToken cancellationToken = default);
    }
}
