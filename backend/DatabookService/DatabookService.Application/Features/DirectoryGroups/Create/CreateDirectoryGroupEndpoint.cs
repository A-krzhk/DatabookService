using DatabookService.Application.DTOs;
using DatabookService.Application.Features.DirectoryGroups.Create;
using DatabookService.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DatabookService.Application.Features.DirectoryGroups.Create;

public class CreateDirectoryGroupEndpoint : IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        var group = app.MapGroup("/api/directory-groups")
            .WithTags("Directory Groups")
            .RequireAuthorization("AdminOnly");

        group.MapPost("/", CreateDirectoryGroup)
            .WithName("CreateDirectoryGroup")
            .Produces<DirectoryGroupDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> CreateDirectoryGroup(
        [FromBody] CreateDirectoryGroupDto dto,
        [FromServices] CreateDirectoryGroupCommand command,
        CancellationToken cancellationToken)
    {
        var result = await command.ExecuteAsync(dto, cancellationToken);
        return Results.Created($"/api/directory-groups/{result.Id}", result);
    }
}
