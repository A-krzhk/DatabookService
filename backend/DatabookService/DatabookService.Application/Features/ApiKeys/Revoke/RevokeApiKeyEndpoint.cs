using DatabookService.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DatabookService.Application.Features.ApiKeys.Revoke;

public class RevokeApiKeyEndpoint : IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        var group = app.MapGroup("/api/keys")
            .WithTags("API Keys")
            .RequireAuthorization("AdminOnly");

        group.MapPatch("/revoke/{id:guid}", Handle)
            .WithName("RevokeApiKey")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Handle(
        [FromRoute] Guid id,
        [FromServices] RevokeApiKeyCommand command,
        CancellationToken ct)
    {
        var ok = await command.ExecuteAsync(id, ct);
        if (!ok) return Results.NotFound();
        return Results.NoContent();
    }
}




