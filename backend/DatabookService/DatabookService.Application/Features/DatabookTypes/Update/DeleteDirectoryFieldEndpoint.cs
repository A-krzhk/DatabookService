using DatabookService.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DatabookService.Application.Features.DatabookTypes.Update;

public class DeleteDirectoryFieldEndpoint : IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        var group = app.MapGroup("/api/directory-types")
            .WithTags("Directory Types")
            .RequireAuthorization("AdminOnly");

        group.MapDelete("/{id:guid}/fields/{fieldId:guid}", DeleteDirectoryField)
            .WithName("DeleteDirectoryField")
            .WithSummary("Delete a field (column) from a directory type")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> DeleteDirectoryField(
        [FromRoute] Guid id,
        [FromRoute] Guid fieldId,
        [FromServices] DeleteDirectoryFieldCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            await command.ExecuteAsync(id, fieldId, cancellationToken);
            return Results.NoContent();
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


