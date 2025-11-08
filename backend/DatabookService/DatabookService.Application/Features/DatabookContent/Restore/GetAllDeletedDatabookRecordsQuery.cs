using DatabookService.Application.DTOs.GetDatabookRecords;
using DatabookService.Application.Interfaces;
using DatabookService.Application.Interfaces.Services;
using DatabookService.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DatabookService.Application.Features.DatabookContent.Restore;

public class GetAllDeletedDatabookRecordsQuery
    {
        private readonly IDirectoryTypeRepository _directoryTypeRepository;
        private readonly IDatabookContentService _databookContentService;
        private readonly ILogger<GetAllDeletedDatabookRecordsQuery> _logger;

        public GetAllDeletedDatabookRecordsQuery(
            IDirectoryTypeRepository directoryTypeRepository,
            IDatabookContentService databookContentService,
            ILogger<GetAllDeletedDatabookRecordsQuery> logger)
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
                {
                    _logger.LogWarning("Directory type with id {TableId} not found", tableId);
                    return Results.NotFound(new { Message = "Directory type not found" });
                }

                _logger.LogInformation("Getting deleted records for table {TableName}", directoryType.TableName);

                // Получаем удаленные данные
                var data = await _databookContentService.GetAllDeletedRecordsAsync(
                    directoryType.TableName,
                    directoryType.Fields,
                    pageNumber,
                    pageSize,
                    cancellationToken);

                // Получаем общее количество удаленных записей для пагинации
                var totalCount = await _databookContentService.GetTotalDeletedCountAsync(
                    directoryType.TableName,
                    cancellationToken);

                // Создаем метаданные колонок (включая служебные поля)
                var columns = new List<ColumnMetadataResponse>
                {
                    new ColumnMetadataResponse(
                        FieldName: "Id",
                        DisplayName: "ID",
                        DataType: "Guid",
                        IsCollection: false,
                        IsRequired: true,
                        MaxLength: null,
                        Reference: null
                    ),
                    new ColumnMetadataResponse(
                        FieldName: "CreatedAt",
                        DisplayName: "Created At",
                        DataType: "DateTime",
                        IsCollection: false,
                        IsRequired: true,
                        MaxLength: null,
                        Reference: null
                    ),
                    new ColumnMetadataResponse(
                        FieldName: "UpdatedAt",
                        DisplayName: "Updated At",
                        DataType: "DateTime",
                        IsCollection: false,
                        IsRequired: false,
                        MaxLength: null,
                        Reference: null
                    ),
                    new ColumnMetadataResponse(
                        FieldName: "DeletedDate",
                        DisplayName: "Deleted Date",
                        DataType: "DateTime",
                        IsCollection: false,
                        IsRequired: false,
                        MaxLength: null,
                        Reference: null
                    )
                };

                // Добавляем пользовательские поля
                columns.AddRange(directoryType.Fields.Select(f => new ColumnMetadataResponse(
                    FieldName: f.ColumnName,
                    DisplayName: f.Name,
                    DataType: f.DataType.ToString(),
                    IsCollection: f.IsCollection,
                    IsRequired: f.IsRequired,
                    MaxLength: f.DataType == FieldDataType.String ? 500 : null,
                    Reference: f.ReferenceDirectoryType != null ? new ReferenceInfoResponse(
                        DirectoryTypeId: f.ReferenceDirectoryType.Id,
                        TableName: f.ReferenceDirectoryType.TableName
                    ) : null
                )));

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
                _logger.LogError(ex, "Error getting deleted databook records for table {TableId}", tableId);
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Internal server error"
                );
            }
        }
    }