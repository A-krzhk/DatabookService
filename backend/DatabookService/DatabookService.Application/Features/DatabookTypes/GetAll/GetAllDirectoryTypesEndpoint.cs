using DatabookService.Application.DTOs;
using DatabookService.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DatabookService.Application.Features.DatabookTypes.GetAll;

public class GetAllDirectoryTypesEndpoint : IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        var group = app.MapGroup("/api/directory-types")
            .WithTags("Directory Types")
            .RequireAuthorization("Contributor");

        group.MapGet("/", GetAllDirectoryTypes)
            .WithName("GetAllDirectoryTypes")
            .Produces<List<DirectoryTypeDto>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> GetAllDirectoryTypes(
        [FromServices] GetAllDirectoryTypesQuery query,
        CancellationToken cancellationToken)
    {
        var result = await query.ExecuteAsync(cancellationToken);
        return Results.Ok(result);
    }
}