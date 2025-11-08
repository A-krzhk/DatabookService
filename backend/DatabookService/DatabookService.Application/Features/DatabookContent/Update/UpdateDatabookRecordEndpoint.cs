using DatabookService.Application.DTOs;
using DatabookService.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace DatabookService.Application.Features.DatabookContent.Update;

public class UpdateDatabookRecordEndpoint : IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        var group = app.MapGroup("/api/directory-record")
            .WithTags("Directory Records")
            .RequireAuthorization("AdminOnly");

        group.MapPut("/{tableId:guid}/{recordId:guid}", UpdateRecord)
            .WithName("UpdateDatabookRecord")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> UpdateRecord(
        [FromRoute] Guid tableId,
        [FromRoute] Guid recordId,
        [FromBody] UpdateDatabookRecordDto dto,
        [FromServices] UpdateDatabookRecordCommand command,
        CancellationToken cancellationToken)
    {
        var payload = dto?.FieldsValues ?? new Dictionary<string, object>();
        return await command.ExecuteAsync(tableId, recordId, payload, cancellationToken);
    }
}
