using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;

namespace AiMapping.Api.Services;

public sealed class XlsxTabularFileParser
{
    private static readonly XNamespace SpreadsheetNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace DocumentRelationshipNamespace = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationshipNamespace = "http://schemas.openxmlformats.org/package/2006/relationships";

    public Task<IReadOnlyList<IReadOnlyList<string>>> ParseAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
        var sharedStrings = ReadSharedStrings(archive);
        var worksheetEntry = ResolveFirstWorksheet(archive);

        if (worksheetEntry is null)
        {
            throw new InvalidOperationException("The XLSX file does not contain a worksheet.");
        }

        using var worksheetStream = worksheetEntry.Open();
        var worksheet = XDocument.Load(worksheetStream);
        var rows = new List<IReadOnlyList<string>>();

        foreach (var rowElement in worksheet.Descendants(SpreadsheetNamespace + "sheetData").Elements(SpreadsheetNamespace + "row"))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cells = new List<string>();
            var nextColumnIndex = 0;

            foreach (var cellElement in rowElement.Elements(SpreadsheetNamespace + "c"))
            {
                var reference = cellElement.Attribute("r")?.Value;
                var columnIndex = reference is null ? nextColumnIndex : GetColumnIndex(reference);

                while (cells.Count < columnIndex)
                {
                    cells.Add(string.Empty);
                }

                cells.Add(ReadCellValue(cellElement, sharedStrings));
                nextColumnIndex = columnIndex + 1;
            }

            rows.Add(TrimTrailingEmptyCells(cells));
        }

        return Task.FromResult((IReadOnlyList<IReadOnlyList<string>>)rows
            .Where(row => row.Any(cell => !string.IsNullOrWhiteSpace(cell)))
            .ToList());
    }

    private static ZipArchiveEntry? ResolveFirstWorksheet(ZipArchive archive)
    {
        var workbookEntry = archive.GetEntry("xl/workbook.xml");
        var workbookRelationshipsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels");

        if (workbookEntry is not null && workbookRelationshipsEntry is not null)
        {
            using var workbookStream = workbookEntry.Open();
            using var relsStream = workbookRelationshipsEntry.Open();

            var workbook = XDocument.Load(workbookStream);
            var relationships = XDocument.Load(relsStream);

            var firstSheet = workbook.Root?
                .Element(SpreadsheetNamespace + "sheets")?
                .Elements(SpreadsheetNamespace + "sheet")
                .FirstOrDefault();

            var relationshipId = firstSheet?.Attribute(DocumentRelationshipNamespace + "id")?.Value;

            if (!string.IsNullOrWhiteSpace(relationshipId))
            {
                var target = relationships.Root?
                    .Elements(PackageRelationshipNamespace + "Relationship")
                    .FirstOrDefault(relationship => relationship.Attribute("Id")?.Value == relationshipId)?
                    .Attribute("Target")?
                    .Value;

                if (!string.IsNullOrWhiteSpace(target))
                {
                    var worksheetPath = ResolveWorkbookTarget(target);
                    return archive.GetEntry(worksheetPath);
                }
            }
        }

        return archive.Entries
            .Where(entry => entry.FullName.StartsWith("xl/worksheets/sheet", StringComparison.OrdinalIgnoreCase)
                && entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => entry.FullName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static string ResolveWorkbookTarget(string target)
    {
        var normalized = target.Replace('\\', '/');
        if (normalized.StartsWith('/'))
        {
            return normalized.TrimStart('/');
        }

        return normalized.StartsWith("xl/", StringComparison.OrdinalIgnoreCase)
            ? normalized
            : $"xl/{normalized}";
    }

    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
        {
            return Array.Empty<string>();
        }

        using var stream = entry.Open();
        var document = XDocument.Load(stream);

        var sharedStrings = document.Root?
            .Elements(SpreadsheetNamespace + "si")
            .Select(item => string.Concat(item.Descendants(SpreadsheetNamespace + "t").Select(text => text.Value)))
            .ToList();

        return sharedStrings is null ? Array.Empty<string>() : sharedStrings;
    }

    private static string ReadCellValue(XElement cellElement, IReadOnlyList<string> sharedStrings)
    {
        var type = cellElement.Attribute("t")?.Value;

        if (type == "inlineStr")
        {
            return string.Concat(cellElement.Descendants(SpreadsheetNamespace + "t").Select(text => text.Value)).Trim();
        }

        var rawValue = cellElement.Element(SpreadsheetNamespace + "v")?.Value;
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return string.Empty;
        }

        return type switch
        {
            "s" when int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)
                && index >= 0
                && index < sharedStrings.Count => sharedStrings[index].Trim(),
            "b" => rawValue == "1" ? "TRUE" : "FALSE",
            _ => rawValue.Trim()
        };
    }

    private static int GetColumnIndex(string cellReference)
    {
        var columnIndex = 0;

        foreach (var ch in cellReference)
        {
            if (!char.IsLetter(ch))
            {
                break;
            }

            columnIndex *= 26;
            columnIndex += char.ToUpperInvariant(ch) - 'A' + 1;
        }

        return Math.Max(0, columnIndex - 1);
    }

    private static IReadOnlyList<string> TrimTrailingEmptyCells(IReadOnlyList<string> row)
    {
        var lastValueIndex = row.Count - 1;
        while (lastValueIndex >= 0 && string.IsNullOrWhiteSpace(row[lastValueIndex]))
        {
            lastValueIndex--;
        }

        return row.Take(lastValueIndex + 1).ToList();
    }
}
