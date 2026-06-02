using System.Globalization;

namespace AiMapping.Api.Services;

public sealed class HeaderRowDetector
{
    private const int MaxRowsToInspect = 15;

    public int DetectHeaderRow(IReadOnlyList<IReadOnlyList<string>> rows)
    {
        if (rows.Count == 0)
        {
            throw new InvalidOperationException("The uploaded file does not contain any rows.");
        }

        var candidateCount = Math.Min(MaxRowsToInspect, rows.Count);
        var bestRowIndex = 0;
        var bestScore = double.MinValue;

        for (var rowIndex = 0; rowIndex < candidateCount; rowIndex++)
        {
            var score = ScoreCandidate(rows, rowIndex);
            if (score > bestScore)
            {
                bestScore = score;
                bestRowIndex = rowIndex;
            }
        }

        return bestRowIndex;
    }

    private static double ScoreCandidate(IReadOnlyList<IReadOnlyList<string>> rows, int rowIndex)
    {
        var row = rows[rowIndex];
        var nonEmptyCells = row
            .Select(cell => cell.Trim())
            .Where(cell => !string.IsNullOrWhiteSpace(cell))
            .ToList();

        if (nonEmptyCells.Count == 0)
        {
            return double.MinValue;
        }

        var uniqueRatio = nonEmptyCells
            .Select(Normalize)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() / (double)nonEmptyCells.Count;

        var textRatio = nonEmptyCells.Count(IsLikelyHeaderText) / (double)nonEmptyCells.Count;
        var numericRatio = nonEmptyCells.Count(IsLikelyValue) / (double)nonEmptyCells.Count;
        var nextRowsScore = ScoreRowsAfterCandidate(rows, rowIndex, nonEmptyCells.Count);
        var precedingPenalty = rowIndex == 0 ? 0 : Math.Min(1.5, rowIndex * 0.15);

        return (nonEmptyCells.Count * 1.8)
            + (uniqueRatio * 2.5)
            + (textRatio * 3.0)
            + nextRowsScore
            - (numericRatio * 2.5)
            - precedingPenalty;
    }

    private static double ScoreRowsAfterCandidate(IReadOnlyList<IReadOnlyList<string>> rows, int rowIndex, int headerCellCount)
    {
        var dataRows = rows
            .Skip(rowIndex + 1)
            .Take(5)
            .Where(row => row.Any(cell => !string.IsNullOrWhiteSpace(cell)))
            .ToList();

        if (dataRows.Count == 0)
        {
            return 0;
        }

        var averageFilledCells = dataRows.Average(row => row.Count(cell => !string.IsNullOrWhiteSpace(cell)));
        var shapeScore = 1 - Math.Min(1, Math.Abs(averageFilledCells - headerCellCount) / Math.Max(1, headerCellCount));
        var valueRows = dataRows.Count(row => row.Any(IsLikelyValue));

        return (shapeScore * 2.0) + (valueRows / (double)dataRows.Count * 1.5);
    }

    private static bool IsLikelyHeaderText(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return false;
        }

        var letterCount = trimmed.Count(char.IsLetter);
        var digitCount = trimmed.Count(char.IsDigit);
        return letterCount > 0 && digitCount <= Math.Max(2, letterCount / 2);
    }

    private static bool IsLikelyValue(string value)
    {
        return decimal.TryParse(value, NumberStyles.Number | NumberStyles.Currency, CultureInfo.InvariantCulture, out _)
            || decimal.TryParse(value, NumberStyles.Number | NumberStyles.Currency, CultureInfo.CurrentCulture, out _)
            || DateOnly.TryParse(value, CultureInfo.InvariantCulture, out _)
            || value.Contains('@', StringComparison.Ordinal);
    }

    private static string Normalize(string value)
    {
        return new string(value
            .Trim()
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }
}
