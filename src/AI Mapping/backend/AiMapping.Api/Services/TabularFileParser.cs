using Microsoft.Extensions.Options;
using AiMapping.Api.Options;

namespace AiMapping.Api.Services;

public sealed class TabularFileParser
{
    private readonly CsvTabularFileParser _csvParser;
    private readonly XlsxTabularFileParser _xlsxParser;
    private readonly ImportOptions _options;

    public TabularFileParser(
        CsvTabularFileParser csvParser,
        XlsxTabularFileParser xlsxParser,
        IOptions<ImportOptions> options)
    {
        _csvParser = csvParser;
        _xlsxParser = xlsxParser;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<IReadOnlyList<string>>> ParseAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length <= 0)
        {
            throw new InvalidOperationException("The uploaded file is empty.");
        }

        if (file.Length > _options.MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"The uploaded file is larger than the {_options.MaxFileSizeBytes / 1024 / 1024} MB limit.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        using var stream = file.OpenReadStream();

        return extension switch
        {
            ".csv" => await _csvParser.ParseAsync(stream, cancellationToken),
            ".xlsx" => await _xlsxParser.ParseAsync(stream, cancellationToken),
            _ => throw new InvalidOperationException("Only CSV and XLSX files are supported.")
        };
    }
}
