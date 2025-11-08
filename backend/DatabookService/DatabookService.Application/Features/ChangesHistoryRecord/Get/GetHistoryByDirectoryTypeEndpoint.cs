using DatabookService.Application.DTOs.GetHistoryRecords;
using DatabookService.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabookService.Application.Features.ChangesHistoryRecord.Get
{
    public class GetHistoryByDirectoryTypeEndpoint : IEndpoint
    {
        public void MapEndpoint(WebApplication app)
        {
            var group = app.MapGroup("/api/history")
                .WithTags("History")
                .RequireAuthorization("AdminOnly");

            group.MapGet("/directory-type/{directoryTypeId}", GetHistoryByDirectoryType)
                .WithName("GetHistoryByDirectoryType")
                .WithSummary("Get history of changes for directory type")
                .Produces<HistoryResponse>(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound);
        }

        private static async Task<IResult> GetHistoryByDirectoryType(
            [FromRoute] Guid directoryTypeId,
            [FromServices] GetHistoryByDirectoryTypeQuery query,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] Guid? recordId = null,           
            CancellationToken cancellationToken = default)
        {
            var request = new HistoryPaginationRequest(
                pageNumber,
                pageSize
            );

            return await query.ExecuteAsync(directoryTypeId, request, cancellationToken);
        }
    }
}
