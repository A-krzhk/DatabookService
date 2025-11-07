using DatabookService.Application.DTOs;
using DatabookService.Application.DTOs.UpdateDirectoryTypes;
using DatabookService.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DatabookService.Application.Features.DatabookTypes.Update;

public class AddFieldEndpoint : IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        var group = app.MapGroup("/api/directory-types")
            .WithTags("Directory Types")
            .RequireAuthorization("AdminOnly");

        group.MapPost("/{directoryTypeId:guid}/fields", AddField)
            .WithName("AddField")
            .Produces<DirectoryFieldDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> AddField(
        [FromRoute] Guid directoryTypeId,
        [FromBody] AddFieldDto dto,
        [FromServices] AddFieldCommand command,
        CancellationToken cancellationToken)
    {
        var result = await command.ExecuteAsync(directoryTypeId, dto, cancellationToken);
        return Results.Created($"/api/directory-types/{directoryTypeId}/fields/{result.Id}", result);
    }
}