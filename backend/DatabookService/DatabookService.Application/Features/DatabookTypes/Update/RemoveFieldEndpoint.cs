using DatabookService.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DatabookService.Application.Features.DatabookTypes.Update;

public class RemoveFieldEndpoint : IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        var group = app.MapGroup("/api/directory-types")
            .WithTags("Directory Types")
            .RequireAuthorization("AdminOnly");

        group.MapDelete("/{directoryTypeId:guid}/fields/{fieldId:guid}", RemoveField)
            .WithName("RemoveField")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> RemoveField(
        [FromRoute] Guid directoryTypeId,
        [FromRoute] Guid fieldId,
        [FromServices] RemoveFieldCommand command,
        CancellationToken cancellationToken)
    {
        await command.ExecuteAsync(directoryTypeId, fieldId, cancellationToken);
        return Results.NoContent();
    }
}