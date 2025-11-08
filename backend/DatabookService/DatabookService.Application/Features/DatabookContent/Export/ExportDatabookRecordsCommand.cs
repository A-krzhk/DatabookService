using DatabookService.Application.Interfaces;
using DatabookService.Application.Interfaces.Services;
using DatabookService.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;

namespace DatabookService.Application.Features.DatabookContent.Export;

public class ExportDatabookRecordsCommand
{
    private readonly IDirectoryTypeRepository _directoryTypeRepository;
    private readonly IDatabookContentService _databookContentService;
    private readonly ILogger<ExportDatabookRecordsCommand> _logger;

    public ExportDatabookRecordsCommand(
        IDirectoryTypeRepository directoryTypeRepository,
        IDatabookContentService databookContentService,
        ILogger<ExportDatabookRecordsCommand> logger)
    {
        _directoryTypeRepository = directoryTypeRepository;
        _databookContentService = databookContentService;
        _logger = logger;
    }

    public async Task<ExportFileResult> ExecuteAsync(
        Guid tableId,
        CancellationToken cancellationToken = default)
    {
        var directoryType = await _directoryTypeRepository.GetByIdAsync(tableId, cancellationToken);
        if (directoryType == null)
        {
            throw new InvalidOperationException($"Directory type with id '{tableId}' not found.");
        }

        var orderedFields = directoryType.Fields
            .OrderBy(f => f.Order)
            .ToList();

        var records = await _databookContentService.GetAllRecordsAsync(
            directoryType.TableName,
            directoryType.Fields,
            cancellationToken: cancellationToken);

        var headers = new List<string> { "Id" };
        headers.AddRange(orderedFields.Select(f => f.ColumnName));

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));

        foreach (var record in records)
        {
            var lineValues = new List<string>
            {
                EscapeCsv(ConvertValue(record.TryGetValue("Id", out var idValue) ? idValue : null))
            };

            foreach (var field in orderedFields)
            {
                record.TryGetValue(field.ColumnName, out var value);
                var formatted = FormatFieldValue(field, value);
                lineValues.Add(EscapeCsv(formatted));
            }

            sb.AppendLine(string.Join(",", lineValues));
        }

        var utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        var bytes = utf8WithBom.GetBytes(sb.ToString());
        var fileName = $"{directoryType.TableName}_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
        _logger.LogInformation("Exported {Count} records from {Table}", records.Count, directoryType.TableName);
        return new ExportFileResult(fileName, bytes, "text/csv; charset=utf-8");
    }

    private static string FormatFieldValue(DirectoryField field, object? value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        if (field.IsCollection)
        {
            if (value is IEnumerable<object> objects)
            {
                return string.Join("|", objects.Select(ConvertValue));
            }

            if (value is IEnumerable<string> strings)
            {
                return string.Join("|", strings);
            }

            return ConvertValue(value);
        }

        return ConvertValue(value);
    }

    private static string ConvertValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            DateTime dateTime => dateTime.Kind == DateTimeKind.Unspecified
                ? dateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : dateTime.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
            bool boolValue => boolValue ? "true" : "false",
            Guid guid => guid.ToString(),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains('"'))
        {
            value = value.Replace("\"", "\"\"");
        }

        if (value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value}\"";
        }

        return value;
    }
}

public readonly record struct ExportFileResult(
    string FileName,
    byte[] Content,
    string ContentType);
