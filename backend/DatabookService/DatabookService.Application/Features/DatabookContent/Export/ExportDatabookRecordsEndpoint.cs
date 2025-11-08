using DatabookService.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DatabookService.Application.Features.DatabookContent.Export;

public class ExportDatabookRecordsEndpoint : IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        var group = app.MapGroup("/api/directory-record")
            .WithTags("Directory Records")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/{tableId:guid}/export", ExportRecords)
            .WithName("ExportDirectoryRecords")
            .Produces(StatusCodes.Status200OK);
    }

    private static async Task<IResult> ExportRecords(
        [FromRoute] Guid tableId,
        [FromServices] ExportDatabookRecordsCommand command,
        CancellationToken cancellationToken)
    {
        var result = await command.ExecuteAsync(tableId, cancellationToken);
        return Results.File(result.Content, result.ContentType, result.FileName);
    }
}
