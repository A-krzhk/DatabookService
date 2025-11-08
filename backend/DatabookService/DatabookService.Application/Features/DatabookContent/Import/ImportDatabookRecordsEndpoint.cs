using DatabookService.Application.DTOs;
using DatabookService.Application.Interfaces;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DatabookService.Application.Features.DatabookContent.Import;

public class ImportDatabookRecordsEndpoint : IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        var group = app.MapGroup("/api/directory-record")
            .WithTags("Directory Records")
            .RequireAuthorization("AdminOnly");

        group.MapPost("/{tableId:guid}/import", ImportRecords)
            .WithName("ImportDirectoryRecords")
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<ImportResultDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .DisableAntiforgery()
            .ExcludeFromDescription();
    }

    private static async Task<IResult> ImportRecords(
        [FromRoute] Guid tableId,
        [FromForm] IFormFile file,
        [FromServices] ImportDatabookRecordsCommand command,
        CancellationToken cancellationToken)
    {
        if (file == null)
        {
            return Results.BadRequest("File is required.");
        }

        var result = await command.ExecuteAsync(tableId, file, cancellationToken);
        return Results.Ok(result);
    }
}
