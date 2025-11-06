using DatabookService.Application.DTOs;
using DatabookService.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DatabookService.Application.Features.DatabookTypes.Update;

public class UpdateDirectoryFieldEndpoint : IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        var group = app.MapGroup("/api/directory-types")
            .WithTags("Directory Types")
            .RequireAuthorization("AdminOnly");

        group.MapPatch("/{id:guid}/fields/{fieldId:guid}", UpdateDirectoryField)
            .WithName("UpdateDirectoryField")
            .WithSummary("Update field (column) of a directory type")
            .Produces<DirectoryTypeDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> UpdateDirectoryField(
        [FromRoute] Guid id,
        [FromRoute] Guid fieldId,
        [FromBody] UpdateDirectoryFieldDto dto,
        [FromServices] UpdateDirectoryFieldCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await command.ExecuteAsync(id, fieldId, dto, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}


