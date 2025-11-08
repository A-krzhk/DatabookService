using DatabookService.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DatabookService.Application.Features.DatabookContent.Restore;

public class RestoreDatabookRecordEndpoint : IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        var group = app.MapGroup("/api/directory-record")
            .WithTags("Directory Records")
            .RequireAuthorization("AdminOnly");

        group.MapPost("/{tableId:guid}/restore/{recordId:guid}", RestoreRecord)
            .WithName("RestoreDatabookRecord")
            .WithDescription("Restore a soft-deleted record")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> RestoreRecord(
        [FromRoute] Guid tableId,
        [FromRoute] Guid recordId,
        [FromServices] RestoreDatabookRecordCommand command,
        CancellationToken cancellationToken)
    {
        return await command.ExecuteAsync(tableId, recordId, cancellationToken);
    }
}