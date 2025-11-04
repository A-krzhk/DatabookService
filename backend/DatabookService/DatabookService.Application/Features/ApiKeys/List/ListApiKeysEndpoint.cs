using DatabookService.Application.DTOs.ApiKeys;
using DatabookService.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DatabookService.Application.Features.ApiKeys.List;

public class ListApiKeysEndpoint : IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        var group = app.MapGroup("/api/keys")
            .WithTags("API Keys")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", Handle)
            .WithName("ListApiKeys")
            .Produces<List<ApiKeyItemDto>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> Handle(
        [FromServices] ListApiKeysQuery query,
        CancellationToken ct)
    {
        var result = await query.ExecuteAsync(ct);
        return Results.Ok(result);
    }
}


