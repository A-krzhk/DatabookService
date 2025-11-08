using DatabookService.Application.DTOs;
using DatabookService.Application.Interfaces;
using DatabookService.Application.Interfaces.Services;
using DatabookService.Domain.Entities;
using DatabookService.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
using System.Text;
using System.Text.Json;

namespace DatabookService.Application.Features.DatabookContent.Import;

public class ImportDatabookRecordsCommand
{
    private readonly IDirectoryTypeRepository _directoryTypeRepository;
    private readonly IDatabookContentService _databookContentService;
    private readonly ITypesValidationService _typesValidationService;
    private readonly ILogger<ImportDatabookRecordsCommand> _logger;

    public ImportDatabookRecordsCommand(
        IDirectoryTypeRepository directoryTypeRepository,
        IDatabookContentService databookContentService,
        ITypesValidationService typesValidationService,
        ILogger<ImportDatabookRecordsCommand> logger)
    {
        _directoryTypeRepository = directoryTypeRepository;
        _databookContentService = databookContentService;
        _typesValidationService = typesValidationService;
        _logger = logger;
    }

    public async Task<ImportResultDto> ExecuteAsync(
        Guid tableId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new InvalidOperationException("CSV file is missing or empty.");
        }

        var directoryType = await _directoryTypeRepository.GetByIdAsync(tableId, cancellationToken);
        if (directoryType == null)
        {
            throw new InvalidOperationException($"Directory type with id '{tableId}' not found.");
        }

        var fieldLookup = CreateFieldLookup(directoryType.Fields);

        var imported = 0;
        var skipped = 0;
        var errors = new List<string>();

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        await using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        var encoding = DetectEncoding(bytes);

        await using var csvStream = new MemoryStream(bytes);
        using var reader = new StreamReader(csvStream, encoding, detectEncodingFromByteOrderMarks: true);
        using var parser = new TextFieldParser(reader)
        {
            TextFieldType = FieldType.Delimited
        };

        parser.SetDelimiters(",");
        parser.HasFieldsEnclosedInQuotes = true;

        var headers = parser.ReadFields();
        if (headers == null || headers.Length == 0)
        {
            throw new InvalidOperationException("CSV file does not contain a header row.");
        }

        var normalizedHeaders = headers
            .Select(h => h?.Trim())
            .Where(h => !string.IsNullOrWhiteSpace(h))
            .ToList();

        var lineNumber = 1; // header

        while (!parser.EndOfData)
        {
            lineNumber++;
            string[]? lineValues;
            try
            {
                lineValues = parser.ReadFields();
            }
            catch (MalformedLineException ex)
            {
                errors.Add($"Line {lineNumber}: {ex.Message}");
                skipped++;
                continue;
            }

            if (lineValues == null)
            {
                skipped++;
                continue;
            }

            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < normalizedHeaders.Count && i < lineValues.Length; i++)
            {
                row[normalizedHeaders[i]] = lineValues[i];
            }

            try
            {
                var payload = BuildPayload(fieldLookup, row, lineNumber);

                var validation = await _typesValidationService.ValidateFields(
                    directoryType.TableName,
                    directoryType.Fields,
                    payload,
                    cancellationToken);

                if (!validation.IsValid)
                {
                    errors.Add($"Line {lineNumber}: {validation.ErrorMessage}");
                    skipped++;
                    continue;
                }

                await _databookContentService.InsertValues(
                    directoryType,
                    directoryType.Fields,
                    payload,
                    cancellationToken);

                imported++;
            }
            catch (Exception ex)
            {
                errors.Add($"Line {lineNumber}: {ex.Message}");
                skipped++;
            }
        }

        return new ImportResultDto(imported, skipped, errors);
    }

    private static Dictionary<string, object> BuildPayload(
        IReadOnlyDictionary<string, DirectoryField> fields,
        IReadOnlyDictionary<string, string> row,
        int lineNumber)
    {
        var payload = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var (columnName, rawValue) in row)
        {
            if (string.Equals(columnName, "Id", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!fields.TryGetValue(columnName, out var field))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                continue;
            }

            if (field.IsCollection)
            {
                var collectionValue = ConvertCollectionValue(field, rawValue, lineNumber);
                payload[field.ColumnName] = collectionValue;
            }
            else
            {
                payload[field.ColumnName] = ConvertSingleValue(field, rawValue, lineNumber);
            }
        }

        return payload;
    }

    private static JsonElement ConvertCollectionValue(
        DirectoryField field,
        string rawValue,
        int lineNumber)
    {
        var separator = new[] { '|' };
        var items = rawValue
            .Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => ConvertScalar(field, value, lineNumber))
            .ToList();

        return JsonSerializer.SerializeToElement(items);
    }

    private static JsonElement ConvertSingleValue(
        DirectoryField field,
        string rawValue,
        int lineNumber)
    {
        var scalar = ConvertScalar(field, rawValue, lineNumber);
        return JsonSerializer.SerializeToElement(scalar);
    }

    private static object ConvertScalar(
        DirectoryField field,
        string rawValue,
        int lineNumber)
    {
        var trimmed = rawValue.Trim();

        return field.DataType switch
        {
            FieldDataType.Number => ParseNumber(trimmed, field.ColumnName, lineNumber),
            FieldDataType.Checkbox => ParseBoolean(trimmed, field.ColumnName, lineNumber),
            FieldDataType.Date => ParseDate(trimmed, field.ColumnName, lineNumber).Date,
            FieldDataType.Datetime => ParseDate(trimmed, field.ColumnName, lineNumber),
            FieldDataType.Reference => ParseGuid(trimmed, field.ColumnName, lineNumber),
            FieldDataType.Enum => ParseEnumValue(field, trimmed, lineNumber),
            _ => trimmed
        };
    }

    private static int ParseNumber(string value, string column, int line)
    {
        if (!int.TryParse(value, out var number))
        {
            throw new InvalidOperationException(
                $"Line {line}: value '{value}' in column '{column}' is not a valid integer.");
        }

        return number;
    }

    private static bool ParseBoolean(string value, string column, int line)
    {
        if (bool.TryParse(value, out var result))
        {
            return result;
        }

        if (value == "1") return true;
        if (value == "0") return false;

        throw new InvalidOperationException(
            $"Line {line}: value '{value}' in column '{column}' is not a valid boolean.");
    }

    private static DateTime ParseDate(string value, string column, int line)
    {
        if (!DateTime.TryParse(value, out var date))
        {
            throw new InvalidOperationException(
                $"Line {line}: value '{value}' in column '{column}' is not a valid date.");
        }

        return date;
    }

    private static Guid ParseGuid(string value, string column, int line)
    {
        if (!Guid.TryParse(value, out var guid))
        {
            throw new InvalidOperationException(
                $"Line {line}: value '{value}' in column '{column}' is not a valid GUID.");
        }

        return guid;
    }

    private static string ParseEnumValue(DirectoryField field, string value, int line)
    {
        if (field.EnumValues == null || field.EnumValues.Count == 0)
        {
            throw new InvalidOperationException(
                $"Line {line}: enum field '{field.ColumnName}' has no configured values.");
        }

        if (!field.EnumValues.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Line {line}: value '{value}' is not allowed for enum field '{field.ColumnName}'.");
        }

        return value;
    }

    private static IReadOnlyDictionary<string, DirectoryField> CreateFieldLookup(
        IEnumerable<DirectoryField> fields)
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var lookup = new Dictionary<string, DirectoryField>(comparer);

        foreach (var field in fields)
        {
            if (!lookup.ContainsKey(field.ColumnName))
            {
                lookup[field.ColumnName] = field;
            }

            if (!string.IsNullOrWhiteSpace(field.Name) &&
                !lookup.ContainsKey(field.Name))
            {
                lookup[field.Name] = field;
            }
        }

        return lookup;
    }

    private static Encoding DetectEncoding(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        }

        try
        {
            var utf8Strict = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            utf8Strict.GetString(bytes);
            return Encoding.UTF8;
        }
        catch (DecoderFallbackException)
        {
            return Encoding.GetEncoding(1251);
        }
    }
}
