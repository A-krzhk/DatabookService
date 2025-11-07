using DatabookService.Application.DTOs;
using DatabookService.Application.Features.DatabookTypes.Create;
using DatabookService.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabookService.Application.Features.DatabookContent.Create
{
    public class CreateDatabookRecordEndpoint : IEndpoint
    {
        public void MapEndpoint(WebApplication app)
        {
            var group = app.MapGroup("/api/databooks")
                .WithTags("Directory Records")
                .RequireAuthorization("AdminOnly");

            group.MapPost("/insert", InsertDatabookRecord)
                .WithName("InsertDatabookRecord")
                .Produces(StatusCodes.Status201Created)
                .Produces(StatusCodes.Status400BadRequest);
        }

        private static async Task<IResult> InsertDatabookRecord(
            [FromBody] CreateDatabookRecordDto dto,
            [FromServices] CreateDatabookRecordCommand command,
            CancellationToken cancellationToken)
        {
            var result = await command.ExecuteAsync(dto, cancellationToken);
            return result;
        }
    }
}
