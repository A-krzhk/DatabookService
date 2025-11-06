using DatabookService.Application.DTOs;
using DatabookService.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DatabookService.Application.Features.DatabookTypes.Update;

public class UpdateDirectoryTypeFieldsEndpoint : IEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        var group = app.MapGroup("/api/directory-types")
            .WithTags("Directory Types")
            .RequireAuthorization("AdminOnly");

        group.MapPut("/{id:guid}/fields", UpdateDirectoryTypeFields)
            .WithName("UpdateDirectoryTypeFields")
            .WithSummary("Update fields (columns) of a directory type")
            .WithDescription("""
                Updates fields of a directory type. Allows adding new fields, removing fields, renaming display names, and changing field order.
                
                **Restrictions:**
                - Cannot change DataType for existing fields
                - Cannot change ColumnName for existing fields
                - Cannot change Id for existing fields
                - Cannot change IsCollection for existing fields
                - Cannot change IsRequired for existing fields
                - Cannot change ReferenceDirectoryTypeId for existing fields
                - Order must be >= 0 and unique
                
                **Example Request:**
                ```json
                {
                  "fields": [
                    {
                      "id": "12345678-1234-1234-1234-123456789abc",
                      "name": "Updated Display Name",
                      "columnName": null,
                      "dataType": null,
                      "isRequired": null,
                      "order": 0,
                      "isCollection": null,
                      "referenceDirectoryTypeId": null
                    },
                    {
                      "id": "87654321-4321-4321-4321-cba987654321",
                      "name": "Another Existing Field",
                      "columnName": null,
                      "dataType": null,
                      "isRequired": null,
                      "order": 1,
                      "isCollection": null,
                      "referenceDirectoryTypeId": null
                    },
                    {
                      "id": null,
                      "name": "New String Field",
                      "columnName": "NewStringColumn",
                      "dataType": 1,
                      "isRequired": false,
                      "order": 2,
                      "isCollection": false,
                      "referenceDirectoryTypeId": null
                    }
                  ]
                }
                ```
                
                **Data Types:**
                - 1 = String
                - 2 = Number
                - 3 = Identifier
                - 4 = Checkbox
                - 5 = Reference (requires referenceDirectoryTypeId)
                
                **Note:** 
                - For existing fields (with id), only `name` and `order` can be changed. All other properties must be null.
                - For new fields (id is null), `columnName`, `dataType`, `isRequired`, and `isCollection` are required.
                - Fields not included in the request will be deleted.
                - Order must be unique and >= 0.
                """)
            .Produces<DirectoryTypeDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> UpdateDirectoryTypeFields(
        [FromRoute] Guid id,
        [FromBody] UpdateDirectoryFieldsDto dto,
        [FromServices] UpdateDirectoryTypeFieldsCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await command.ExecuteAsync(id, dto, cancellationToken);
            return Results.Ok(result);
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


