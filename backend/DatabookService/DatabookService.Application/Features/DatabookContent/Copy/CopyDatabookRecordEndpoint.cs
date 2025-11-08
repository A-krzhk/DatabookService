using DatabookService.Application.DTOs;
using DatabookService.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabookService.Application.Features.DatabookContent.Copy
{
    public class CopyDatabookRecordEndpoint : IEndpoint
    {
        public void MapEndpoint(WebApplication app)
        {
            var group = app.MapGroup("/api/directory-record")
                .WithTags("Directory Records")
                .RequireAuthorization();

            group.MapPost("/copy", CopyRecord)
                .WithName("CopyDatabookRecord")
                .WithSummary("Copy databook record")
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status404NotFound)
                .Produces(StatusCodes.Status400BadRequest);
        }

        private static async Task<IResult> CopyRecord(
            [FromBody] CopyDatabookRecordDto copyDto,
            [FromServices] CopyDatabookRecordCommand command,
            CancellationToken cancellationToken)
        {
            return await command.ExecuteAsync(copyDto, cancellationToken);
        }
    }
}