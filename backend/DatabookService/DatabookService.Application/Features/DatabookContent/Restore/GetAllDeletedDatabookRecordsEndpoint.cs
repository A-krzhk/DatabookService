using DatabookService.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DatabookService.Application.Features.DatabookContent.Restore;

public class GetAllDeletedDatabookRecordsEndpoint : IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        var group = app.MapGroup("/api/directory-record")
            .WithTags("Directory Records")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/{tableId:guid}/deleted", GetAllDeletedRecords)
            .WithName("GetAllDeletedDatabookRecords")
            .WithDescription("Get all deleted records from a directory table with pagination support")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> GetAllDeletedRecords(
        [FromRoute] Guid tableId,
        [FromServices] GetAllDeletedDatabookRecordsQuery query,
        CancellationToken cancellationToken,
        [FromQuery] int? page = null,
        [FromQuery] int? size = null)
    {
        return await query.ExecuteAsync(tableId, page, size, cancellationToken);
    }
}