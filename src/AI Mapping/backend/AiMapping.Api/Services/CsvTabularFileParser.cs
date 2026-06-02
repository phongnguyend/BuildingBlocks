using System.Text;

namespace AiMapping.Api.Services;

public sealed class CsvTabularFileParser
{
    public async Task<IReadOnlyList<IReadOnlyList<string>>> ParseAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var text = await reader.ReadToEndAsync(cancellationToken);
        var delimiter = DetectDelimiter(text);

        return Parse(text, delimiter)
            .Where(row => row.Any(cell => !string.IsNullOrWhiteSpace(cell)))
            .Select(row => (IReadOnlyList<string>)TrimTrailingEmptyCells(row))
            .ToList();
    }

    private static char DetectDelimiter(string text)
    {
        var firstLine = text
            .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None)
            .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line)) ?? string.Empty;

        var candidates = new[] { ',', ';', '\t' };
        return candidates
            .Select(delimiter => new { Delimiter = delimiter, Count = CountDelimiter(firstLine, delimiter) })
            .OrderByDescending(candidate => candidate.Count)
            .First().Delimiter;
    }

    private static int CountDelimiter(string line, char delimiter)
    {
        var count = 0;
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    i++;
                    continue;
                }

                inQuotes = !inQuotes;
            }
            else if (ch == delimiter && !inQuotes)
            {
                count++;
            }
        }

        return count;
    }

    private static IReadOnlyList<IReadOnlyList<string>> Parse(string text, char delimiter)
    {
        var rows = new List<IReadOnlyList<string>>();
        var row = new List<string>();
        var cell = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];

            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        cell.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    cell.Append(ch);
                }

                continue;
            }

            if (ch == '"' && cell.Length == 0)
            {
                inQuotes = true;
            }
            else if (ch == delimiter)
            {
                row.Add(cell.ToString().Trim());
                cell.Clear();
            }
            else if (ch == '\r' || ch == '\n')
            {
                if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                row.Add(cell.ToString().Trim());
                cell.Clear();
                rows.Add(row);
                row = new List<string>();
            }
            else
            {
                cell.Append(ch);
            }
        }

        if (cell.Length > 0 || row.Count > 0)
        {
            row.Add(cell.ToString().Trim());
            rows.Add(row);
        }

        return rows;
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
