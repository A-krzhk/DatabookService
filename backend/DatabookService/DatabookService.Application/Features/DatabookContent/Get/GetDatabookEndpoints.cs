using DatabookService.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DatabookService.Application.Features.DatabookContent.Get;

public class GetDatabookEndpoints : IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        var group = app.MapGroup("/api/directory-record")
            .WithTags("Directory Records")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/{tableId:guid}/all", GetAllRecords)
            .WithName("GetAllDatabookRecords")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{tableId:guid}/{recordId:guid}", GetRecordById)
            .WithName("GetDatabookRecordById")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetAllRecords(
        [FromRoute] Guid tableId,
        [FromServices] GetAllDatabookRecordsQuery query,
        CancellationToken cancellationToken,
        [FromQuery] int? page = null,
        [FromQuery] int? size = null
)
    {
        return await query.ExecuteAsync(tableId, page, size, cancellationToken);
    }

    private static async Task<IResult> GetRecordById(
        [FromRoute] Guid tableId,
        [FromRoute] Guid recordId,
        [FromServices] GetDatabookRecordByIdQuery query,
        CancellationToken cancellationToken)
    {
        return await query.ExecuteAsync(tableId, recordId, cancellationToken);
    }
}