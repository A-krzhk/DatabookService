using DatabookService.Application.DTOs;
using DatabookService.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DatabookService.Application.Features.DatabookTypes.Update;

public class UpdateDirectoryTypeGroupEndpoint : IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        var group = app.MapGroup("/api/directory-types")
            .WithTags("Directory Types")
            .RequireAuthorization("AdminOnly");

        group.MapPut("/{directoryTypeId:guid}/group", UpdateGroup)
            .WithName("UpdateDirectoryTypeGroup")
            .Produces<DirectoryTypeDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> UpdateGroup(
        [FromRoute] Guid directoryTypeId,
        [FromBody] UpdateDirectoryTypeGroupDto dto,
        [FromServices] UpdateDirectoryTypeGroupCommand command,
        CancellationToken cancellationToken)
    {
        var result = await command.ExecuteAsync(directoryTypeId, dto.DirectoryGroupId, cancellationToken);
        return Results.Ok(result);
    }
}
