using DatabookService.Application.DTOs;
using DatabookService.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DatabookService.Application.Features.DatabookTypes.Create;

public class CreateDirectoryTypeEndpoint : IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        var group = app.MapGroup("/api/directory-types")
            .WithTags("Directory Types");
            //.RequireAuthorization("AdminOnly");

        group.MapPost("/", CreateDirectoryType)
            .WithName("CreateDirectoryType")
            .Produces<DirectoryTypeDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> CreateDirectoryType(
        [FromBody] CreateDirectoryTypeDto dto,
        [FromServices] CreateDirectoryTypeCommand command,
        CancellationToken cancellationToken)
    {
        var result = await command.ExecuteAsync(dto, cancellationToken);
        return Results.Created($"/api/directory-types/{result.Id}", result);
    }
}
