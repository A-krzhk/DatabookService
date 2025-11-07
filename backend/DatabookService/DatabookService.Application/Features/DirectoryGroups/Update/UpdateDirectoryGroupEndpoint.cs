using DatabookService.Application.DTOs;
using DatabookService.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DatabookService.Application.Features.DirectoryGroups.Update;

public class UpdateDirectoryGroupEndpoint : IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        var group = app.MapGroup("/api/directory-groups")
            .WithTags("Directory Groups")
            .RequireAuthorization("AdminOnly");

        group.MapPut("/{id:guid}", UpdateDirectoryGroup)
            .WithName("UpdateDirectoryGroup")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> UpdateDirectoryGroup(
        [FromRoute] Guid id,
        [FromBody] UpdateDirectoryGroupDto dto,
        [FromServices] UpdateDirectoryGroupCommand command,
        CancellationToken cancellationToken)
    {
        await command.ExecuteAsync(id, dto.Name, cancellationToken);
        return Results.NoContent();
    }
}
