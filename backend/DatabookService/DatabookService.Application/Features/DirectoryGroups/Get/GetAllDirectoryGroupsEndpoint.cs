using DatabookService.Application.DTOs;
using DatabookService.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DatabookService.Application.Features.DirectoryGroups.Get;

public class GetAllDirectoryGroupsEndpoint : IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        var group = app.MapGroup("/api/directory-groups")
            .WithTags("Directory Groups")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", GetAllDirectoryGroups)
            .WithName("GetAllDirectoryGroups")
            .Produces<List<DirectoryGroupDto>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> GetAllDirectoryGroups(
        [FromServices] GetAllDirectoryGroupsQuery command,
        CancellationToken cancellationToken)
    {
        var result = await command.ExecuteAsync(cancellationToken);
        return Results.Ok(result);
    }
}
