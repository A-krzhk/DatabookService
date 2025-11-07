using DatabookService.Application.DTOs;
using DatabookService.Application.DTOs.UpdateDirectoryTypes;
using DatabookService.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DatabookService.Application.Features.DatabookTypes.Update;

public class UpdateFieldEndpoint : IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        var group = app.MapGroup("/api/directory-types")
            .WithTags("Directory Types")
            .RequireAuthorization("AdminOnly");

        group.MapPut("/{directoryTypeId:guid}/fields/{fieldId:guid}", UpdateField)
            .WithName("UpdateField")
            .Produces<DirectoryFieldDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> UpdateField(
        [FromRoute] Guid directoryTypeId,
        [FromRoute] Guid fieldId,
        [FromBody] UpdateFieldDto dto,
        [FromServices] UpdateFieldCommand command,
        CancellationToken cancellationToken)
    {
        var result = await command.ExecuteAsync(directoryTypeId, fieldId, dto, cancellationToken);
        return Results.Ok(result);
    }
}