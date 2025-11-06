using DatabookService.Application.DTOs.ApiKeys;
using DatabookService.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DatabookService.Application.Features.ApiKeys.Create;

public class CreateApiKeyEndpoint : IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        var group = app.MapGroup("/api/keys")
            .WithTags("API Keys");

        group.MapPost("/", Handle)
            .WithName("CreateApiKey")
            .Produces<CreateApiKeyResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .WithSummary("Create API key (returns plain key once)");
    }

    private static async Task<IResult> Handle(
        [FromBody] CreateApiKeyRequest request,
        [FromServices] CreateApiKeyCommand command,
        CancellationToken ct)
    {
        var result = await command.ExecuteAsync(request, ct);
        return Results.Created($"/api/keys/{result.Id}", result);
    }
}




