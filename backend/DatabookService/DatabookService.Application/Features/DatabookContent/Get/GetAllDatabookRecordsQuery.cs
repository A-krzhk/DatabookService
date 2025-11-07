using DatabookService.Application.DTOs;
using DatabookService.Application.DTOs.GetDatabookRecords;
using DatabookService.Application.Interfaces;
using DatabookService.Application.Interfaces.Services;
using DatabookService.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DatabookService.Application.Features.DatabookContent.Get;

public class GetAllDatabookRecordsQuery
{
    private readonly IDirectoryTypeRepository _directoryTypeRepository;
    private readonly IDatabookContentService _databookContentService;
    private readonly ILogger<GetAllDatabookRecordsQuery> _logger;

    public GetAllDatabookRecordsQuery(
        IDirectoryTypeRepository directoryTypeRepository,
        IDatabookContentService databookContentService,
        ILogger<GetAllDatabookRecordsQuery> logger)
    {
        _directoryTypeRepository = directoryTypeRepository;
        _databookContentService = databookContentService;
        _logger = logger;
    }

    public async Task<IResult> ExecuteAsync(
        Guid tableId,
        int? pageNumber = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var directoryType = await _directoryTypeRepository.GetByIdAsync(tableId);
            if (directoryType == null)
                return Results.NotFound("Directory type not found");
            
            // Получаем данные
            var data = await _databookContentService.GetAllRecordsAsync(
                directoryType.TableName,
                directoryType.Fields,
                pageNumber,
                pageSize,
                cancellationToken: cancellationToken);

            // Получаем общее количество для пагинации
            var totalCount = await _databookContentService.GetTotalCountAsync(
                directoryType.TableName, 
                cancellationToken);

            // Создаем метаданные колонок
            var columns = directoryType.Fields.Select(f => new ColumnMetadataResponse(
                FieldName: f.ColumnName,
                DisplayName: f.Name,
                DataType: f.DataType.ToString(),
                IsCollection: f.IsCollection,
                IsRequired: f.IsRequired,
                MaxLength: f.DataType == FieldDataType.String ? 255 : null,
                Reference: f.ReferenceDirectoryType != null ? new ReferenceInfoResponse(
                    DirectoryTypeId: f.ReferenceDirectoryType.Id,
                    TableName: f.ReferenceDirectoryType.TableName
                ) : null
            )).ToList();

            // Создаем пагинацию если нужно
            PaginationResponse? pagination = null;
            if (pageNumber.HasValue && pageSize.HasValue)
            {
                pagination = new PaginationResponse(
                    PageNumber: pageNumber.Value,
                    PageSize: pageSize.Value,
                    TotalCount: totalCount,
                    TotalPages: (int)Math.Ceiling(totalCount / (double)pageSize.Value),
                    HasPrevious: pageNumber > 1,
                    HasNext: pageNumber < (int)Math.Ceiling(totalCount / (double)pageSize.Value)
                );
            }

            // Возвращаем структурированный ответ
            var response = new DatabookRecordsResponse(columns, data, pagination);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting databook records for table {TableId}", tableId);
            return Results.Problem("Internal server error");
        }
    }
}
