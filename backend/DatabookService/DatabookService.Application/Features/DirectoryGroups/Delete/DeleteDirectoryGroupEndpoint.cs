using DatabookService.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DatabookService.Application.Features.DirectoryGroups.Delete;

public class DeleteDirectoryGroupEndpoint : IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        var group = app.MapGroup("/api/directory-groups")
            .WithTags("Directory Groups")
            .RequireAuthorization("AdminOnly");

        group.MapDelete("/{id:guid}", DeleteDirectoryGroup)
            .WithName("DeleteDirectoryGroup")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> DeleteDirectoryGroup(
        [FromRoute] Guid id,
        [FromServices] DeleteDirectoryGroupCommand command,
        CancellationToken cancellationToken)
    {
        await command.ExecuteAsync(id, cancellationToken);
        return Results.NoContent();
    }
}
