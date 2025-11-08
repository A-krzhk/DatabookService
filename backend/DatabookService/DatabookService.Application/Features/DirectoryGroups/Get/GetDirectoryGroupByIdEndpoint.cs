using DatabookService.Application.DTOs;
using DatabookService.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DatabookService.Application.Features.DirectoryGroups.Get;

public class GetDirectoryGroupByIdEndpoint : IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        var group = app.MapGroup("/api/directory-groups")
            .WithTags("Directory Groups")
            .RequireAuthorization("Contributor");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetDirectoryGroupById")
            .Produces<DirectoryGroupDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetById(
        [FromRoute] Guid id,
        [FromServices] GetDirectoryGroupByIdQuery query,
        CancellationToken cancellationToken)
    {
        var dto = await query.ExecuteAsync(id, cancellationToken);
        return dto is null ? Results.NotFound() : Results.Ok(dto);
    }
}