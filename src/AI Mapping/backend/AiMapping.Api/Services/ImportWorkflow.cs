using System.Globalization;
using AiMapping.Api.Models;
using Microsoft.Extensions.Caching.Memory;

namespace AiMapping.Api.Services;

public sealed class ImportWorkflow
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromMinutes(30);

    private readonly TabularFileParser _fileParser;
    private readonly HeaderRowDetector _headerRowDetector;
    private readonly PredefinedHeaderCatalog _headerCatalog;
    private readonly IHeaderMappingSuggester _mappingSuggester;
    private readonly IMemoryCache _cache;

    public ImportWorkflow(
        TabularFileParser fileParser,
        HeaderRowDetector headerRowDetector,
        PredefinedHeaderCatalog headerCatalog,
        IHeaderMappingSuggester mappingSuggester,
        IMemoryCache cache)
    {
        _fileParser = fileParser;
        _headerRowDetector = headerRowDetector;
        _headerCatalog = headerCatalog;
        _mappingSuggester = mappingSuggester;
        _cache = cache;
    }

    public async Task<AnalyzeImportResponse> AnalyzeAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var rows = await _fileParser.ParseAsync(file, cancellationToken);
        if (rows.Count == 0)
        {
            throw new InvalidOperationException("The uploaded file does not contain any data rows.");
        }

        var headerRowIndex = _headerRowDetector.DetectHeaderRow(rows);
        var sourceHeaders = CreateUniqueHeaders(rows[headerRowIndex]);
        var targetHeaders = _headerCatalog.GetHeaders();
        var previewRows = GetPreviewRows(rows, headerRowIndex, sourceHeaders.Count);
        var suggestions = await _mappingSuggester.SuggestAsync(sourceHeaders, targetHeaders, previewRows, cancellationToken);
        var importId = Guid.NewGuid().ToString("N");

        _cache.Set(
            importId,
            new ParsedImport(file.FileName, rows, headerRowIndex, sourceHeaders),
            new MemoryCacheEntryOptions { SlidingExpiration = SessionLifetime });

        return new AnalyzeImportResponse(
            importId,
            file.FileName,
            headerRowIndex,
            sourceHeaders,
            targetHeaders,
            previewRows,
            suggestions);
    }

    public Task<CompleteImportResponse> CompleteAsync(CompleteImportRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ImportId))
        {
            throw new InvalidOperationException("Import id is required.");
        }

        if (!_cache.TryGetValue<ParsedImport>(request.ImportId, out var parsedImport) || parsedImport is null)
        {
            throw new InvalidOperationException("The import session expired. Upload the file again.");
        }

        var targetHeaders = _headerCatalog.GetHeaders();
        var validTargetKeys = targetHeaders.Select(header => header.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var sourceIndexByHeader = parsedImport.Headers
            .Select((header, index) => new { header, index })
            .ToDictionary(item => item.header, item => item.index, StringComparer.OrdinalIgnoreCase);

        var warnings = new List<string>();
        var mappings = request.Mappings
            .Where(mapping => !string.IsNullOrWhiteSpace(mapping.TargetKey))
            .Where(mapping =>
            {
                if (!validTargetKeys.Contains(mapping.TargetKey!))
                {
                    warnings.Add($"Ignoring unknown target field '{mapping.TargetKey}'.");
                    return false;
                }

                if (!sourceIndexByHeader.ContainsKey(mapping.SourceHeader))
                {
                    warnings.Add($"Ignoring unknown source header '{mapping.SourceHeader}'.");
                    return false;
                }

                return true;
            })
            .GroupBy(mapping => mapping.TargetKey!, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();

        var items = new List<CustomerImportDto>();
        var totalRows = 0;
        var skippedRows = 0;

        for (var rowIndex = parsedImport.HeaderRowIndex + 1; rowIndex < parsedImport.Rows.Count; rowIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var row = parsedImport.Rows[rowIndex];
            if (row.All(cell => string.IsNullOrWhiteSpace(cell)))
            {
                skippedRows++;
                continue;
            }

            totalRows++;
            var dto = new CustomerImportDto { SourceRowNumber = rowIndex + 1 };

            foreach (var mapping in mappings)
            {
                var sourceIndex = sourceIndexByHeader[mapping.SourceHeader];
                var value = sourceIndex < row.Count ? row[sourceIndex] : string.Empty;
                ApplyValue(dto, mapping.TargetKey!, value);
            }

            if (HasMappedValue(dto))
            {
                items.Add(dto);
            }
            else
            {
                skippedRows++;
            }
        }

        return Task.FromResult(new CompleteImportResponse(items, totalRows, items.Count, skippedRows, warnings));
    }

    private static IReadOnlyList<string> CreateUniqueHeaders(IReadOnlyList<string> headerRow)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var headers = new List<string>();

        for (var index = 0; index < headerRow.Count; index++)
        {
            var baseHeader = string.IsNullOrWhiteSpace(headerRow[index])
                ? $"Column {index + 1}"
                : headerRow[index].Trim();

            counts.TryGetValue(baseHeader, out var count);
            count++;
            counts[baseHeader] = count;

            headers.Add(count == 1 ? baseHeader : $"{baseHeader} ({count})");
        }

        return headers;
    }

    private static IReadOnlyList<IReadOnlyList<string>> GetPreviewRows(
        IReadOnlyList<IReadOnlyList<string>> rows,
        int headerRowIndex,
        int columnCount)
    {
        return rows
            .Skip(headerRowIndex + 1)
            .Where(row => row.Any(cell => !string.IsNullOrWhiteSpace(cell)))
            .Take(5)
            .Select(row => NormalizeRowLength(row, columnCount))
            .ToList();
    }

    private static IReadOnlyList<string> NormalizeRowLength(IReadOnlyList<string> row, int columnCount)
    {
        var normalized = new string[columnCount];
        for (var index = 0; index < columnCount; index++)
        {
            normalized[index] = index < row.Count ? row[index] : string.Empty;
        }

        return normalized;
    }

    private static void ApplyValue(CustomerImportDto dto, string targetKey, string value)
    {
        var normalizedValue = string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        switch (targetKey)
        {
            case "firstName":
                dto.FirstName = normalizedValue;
                break;
            case "lastName":
                dto.LastName = normalizedValue;
                break;
            case "email":
                dto.Email = normalizedValue;
                break;
            case "phoneNumber":
                dto.PhoneNumber = normalizedValue;
                break;
            case "company":
                dto.Company = normalizedValue;
                break;
            case "jobTitle":
                dto.JobTitle = normalizedValue;
                break;
            case "country":
                dto.Country = normalizedValue;
                break;
            case "city":
                dto.City = normalizedValue;
                break;
            case "signupDate":
                dto.SignupDate = ParseDate(normalizedValue);
                break;
            case "annualRevenue":
                dto.AnnualRevenue = ParseDecimal(normalizedValue);
                break;
        }
    }

    private static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, out var invariantDate))
        {
            return invariantDate;
        }

        if (DateOnly.TryParse(value, CultureInfo.CurrentCulture, out var currentDate))
        {
            return currentDate;
        }

        if (double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var excelSerial)
            && excelSerial > 1
            && excelSerial < 60000)
        {
            return DateOnly.FromDateTime(DateTime.FromOADate(excelSerial));
        }

        return null;
    }

    private static decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (decimal.TryParse(value, NumberStyles.Number | NumberStyles.Currency, CultureInfo.InvariantCulture, out var invariantDecimal))
        {
            return invariantDecimal;
        }

        if (decimal.TryParse(value, NumberStyles.Number | NumberStyles.Currency, CultureInfo.CurrentCulture, out var currentDecimal))
        {
            return currentDecimal;
        }

        return null;
    }

    private static bool HasMappedValue(CustomerImportDto dto)
    {
        return !string.IsNullOrWhiteSpace(dto.FirstName)
            || !string.IsNullOrWhiteSpace(dto.LastName)
            || !string.IsNullOrWhiteSpace(dto.Email)
            || !string.IsNullOrWhiteSpace(dto.PhoneNumber)
            || !string.IsNullOrWhiteSpace(dto.Company)
            || !string.IsNullOrWhiteSpace(dto.JobTitle)
            || !string.IsNullOrWhiteSpace(dto.Country)
            || !string.IsNullOrWhiteSpace(dto.City)
            || dto.SignupDate is not null
            || dto.AnnualRevenue is not null;
    }
}
