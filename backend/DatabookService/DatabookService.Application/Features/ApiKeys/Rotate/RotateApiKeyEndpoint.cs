using DatabookService.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DatabookService.Application.Features.ApiKeys.Rotate;

public class RotateApiKeyEndpoint : IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        var group = app.MapGroup("/api/keys")
            .WithTags("API Keys")
            .RequireAuthorization("AdminOnly");

        group.MapPost("/rotate/{id:guid}", Handle)
            .WithName("RotateApiKey")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithSummary("Rotate API key (returns new plain key once)");
    }

    private static async Task<IResult> Handle(
        [FromRoute] Guid id,
        [FromServices] RotateApiKeyCommand command,
        CancellationToken ct)
    {
        var (found, plain) = await command.ExecuteAsync(id, ct);
        if (!found) return Results.NotFound();
        return Results.Ok(plain);
    }
}


