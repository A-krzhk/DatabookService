using DatabookService.Application.DTOs.GetHistoryRecords;
using DatabookService.Application.Interfaces;
using DatabookService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabookService.Application.Features.ChangesHistoryRecord.Get
{
    public class GetHistoryByDirectoryTypeQuery
    {
        private readonly IChangesHistoryRecordService _changesHistoryService;
        private readonly IDirectoryTypeRepository _directoryTypeRepository;
        private readonly ILogger<GetHistoryByDirectoryTypeQuery> _logger;

        public GetHistoryByDirectoryTypeQuery(
            IChangesHistoryRecordService changesHistoryService,
            IDirectoryTypeRepository directoryTypeRepository,
            ILogger<GetHistoryByDirectoryTypeQuery> logger)
        {
            _changesHistoryService = changesHistoryService;
            _directoryTypeRepository = directoryTypeRepository;
            _logger = logger;
        }

        public async Task<IResult> ExecuteAsync(
            Guid directoryTypeId,
            HistoryPaginationRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Проверяем существование справочника
                var directoryType = await _directoryTypeRepository.GetByIdAsync(directoryTypeId, cancellationToken);
                if (directoryType == null)
                {
                    return Results.NotFound($"Directory type with ID {directoryTypeId} not found");
                }

                var history = await _changesHistoryService.GetHistoryByDirectoryTypeAsync(directoryTypeId, request, cancellationToken);

                return Results.Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting history for directory type {DirectoryTypeId}", directoryTypeId);
                return Results.Problem("Internal server error");
            }
        }
    }
}
