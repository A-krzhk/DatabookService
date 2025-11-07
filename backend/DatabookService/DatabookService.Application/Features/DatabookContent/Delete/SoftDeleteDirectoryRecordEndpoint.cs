using DatabookService.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DatabookService.Application.Features.DatabookContent.Delete;

public class SoftDeleteDirectoryRecordEndpoint : IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        var group = app.MapGroup("/api/directory-records")
            .WithTags("Directory Records")
            .RequireAuthorization("AdminOnly");

        group.MapDelete("/{directoryTypeId:guid}/{recordId:guid}", SoftDeleteRecord)
            .WithName("SoftDeleteDirectoryRecord")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> SoftDeleteRecord(
        Guid directoryTypeId,
        Guid recordId,
        [FromServices] SoftDeleteDirectoryRecordCommand command,
        CancellationToken cancellationToken)
    {
        await command.ExecuteAsync(directoryTypeId, recordId, cancellationToken);
        return Results.NoContent();
    }
}