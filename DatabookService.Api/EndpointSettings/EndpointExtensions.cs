using DatabookService.Application.Commands;
using DatabookService.Application.DTOs;
using DatabookService.Application.Queries;
using Microsoft.AspNetCore.Mvc;

namespace DatabookService.Web.EndpointSettings;

public static class EndpointExtensions
{
    public static void MapDirectoryTypeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/directory-types")
            .WithTags("Directory Types")
            .WithOpenApi();

        // GET: Получить все типы справочников
        group.MapGet("/", async (
                [FromServices] GetAllDirectoryTypesQuery query,
                CancellationToken cancellationToken) =>
            {
                var result = await query.ExecuteAsync(cancellationToken);
                return Results.Ok(result);
            })
            .WithName("GetAllDirectoryTypes")
            .Produces<List<DirectoryTypeDto>>(StatusCodes.Status200OK);

        // POST: Создать новый тип справочника
        group.MapPost("/", async (
                [FromBody] CreateDirectoryTypeDto dto,
                [FromServices] CreateDirectoryTypeCommand command,
                CancellationToken cancellationToken) =>
            {
                var result = await command.ExecuteAsync(dto, cancellationToken);
                return Results.Created($"/api/directory-types/{result.Id}", result);
            })
            .WithName("CreateDirectoryType")
            .Produces<DirectoryTypeDto>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);
    }
}