using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using AiMapping.Api.Models;
using AiMapping.Api.Options;
using Microsoft.Extensions.Options;

namespace AiMapping.Api.Services;

public interface IHeaderMappingSuggester
{
    Task<IReadOnlyList<MappingSuggestion>> SuggestAsync(
        IReadOnlyList<string> sourceHeaders,
        IReadOnlyList<PredefinedHeader> targetHeaders,
        IReadOnlyList<IReadOnlyList<string>> sampleRows,
        CancellationToken cancellationToken);
}

public sealed class HeaderMappingSuggester : IHeaderMappingSuggester
{
    private readonly OpenAiHeaderMappingService _openAiService;
    private readonly FallbackHeaderMappingService _fallbackService;
    private readonly ILogger<HeaderMappingSuggester> _logger;

    public HeaderMappingSuggester(
        OpenAiHeaderMappingService openAiService,
        FallbackHeaderMappingService fallbackService,
        ILogger<HeaderMappingSuggester> logger)
    {
        _openAiService = openAiService;
        _fallbackService = fallbackService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MappingSuggestion>> SuggestAsync(
        IReadOnlyList<string> sourceHeaders,
        IReadOnlyList<PredefinedHeader> targetHeaders,
        IReadOnlyList<IReadOnlyList<string>> sampleRows,
        CancellationToken cancellationToken)
    {
        var fallbackSuggestions = _fallbackService.Suggest(sourceHeaders, targetHeaders, sampleRows);

        if (!_openAiService.IsConfigured)
        {
            return fallbackSuggestions;
        }

        try
        {
            var aiSuggestions = await _openAiService.SuggestAsync(sourceHeaders, targetHeaders, sampleRows, cancellationToken);
            if (aiSuggestions.Count == 0)
            {
                return fallbackSuggestions;
            }

            return MergeWithFallback(sourceHeaders, aiSuggestions, fallbackSuggestions);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "OpenAI header mapping failed. Falling back to deterministic mapping.");
            return fallbackSuggestions;
        }
    }

    private static IReadOnlyList<MappingSuggestion> MergeWithFallback(
        IReadOnlyList<string> sourceHeaders,
        IReadOnlyList<MappingSuggestion> aiSuggestions,
        IReadOnlyList<MappingSuggestion> fallbackSuggestions)
    {
        var aiBySource = aiSuggestions.ToDictionary(suggestion => suggestion.SourceHeader, StringComparer.OrdinalIgnoreCase);
        var fallbackBySource = fallbackSuggestions.ToDictionary(suggestion => suggestion.SourceHeader, StringComparer.OrdinalIgnoreCase);
        var merged = new List<MappingSuggestion>();

        foreach (var sourceHeader in sourceHeaders)
        {
            if (aiBySource.TryGetValue(sourceHeader, out var aiSuggestion))
            {
                merged.Add(aiSuggestion);
            }
            else if (fallbackBySource.TryGetValue(sourceHeader, out var fallbackSuggestion))
            {
                merged.Add(fallbackSuggestion);
            }
        }

        return merged;
    }
}

public sealed class OpenAiHeaderMappingService
{
    private const string IgnoredTargetKey = "__ignore";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;

    public OpenAiHeaderMappingService(HttpClient httpClient, IOptions<OpenAiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<IReadOnlyList<MappingSuggestion>> SuggestAsync(
        IReadOnlyList<string> sourceHeaders,
        IReadOnlyList<PredefinedHeader> targetHeaders,
        IReadOnlyList<IReadOnlyList<string>> sampleRows,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            return Array.Empty<MappingSuggestion>();
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = JsonContent.Create(BuildRequestPayload(_options.Model, sourceHeaders, targetHeaders, sampleRows), mediaType: null, options: JsonOptions);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);
        var outputText = ExtractOutputText(document.RootElement);

        if (string.IsNullOrWhiteSpace(outputText))
        {
            return Array.Empty<MappingSuggestion>();
        }

        var envelope = JsonSerializer.Deserialize<OpenAiMappingEnvelope>(outputText, JsonOptions);
        if (envelope?.Mappings is null)
        {
            return Array.Empty<MappingSuggestion>();
        }

        var validSourceHeaders = sourceHeaders.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var validTargetKeys = targetHeaders.Select(header => header.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return envelope.Mappings
            .Where(mapping => validSourceHeaders.Contains(mapping.SourceHeader))
            .Select(mapping =>
            {
                var targetKey = mapping.TargetKey == IgnoredTargetKey || !validTargetKeys.Contains(mapping.TargetKey)
                    ? null
                    : mapping.TargetKey;

                return new MappingSuggestion(
                    mapping.SourceHeader,
                    targetKey,
                    Math.Clamp(mapping.Confidence, 0, 1),
                    string.IsNullOrWhiteSpace(mapping.Rationale) ? "Suggested by OpenAI." : mapping.Rationale,
                    "OpenAI");
            })
            .ToList();
    }

    private static object BuildRequestPayload(
        string model,
        IReadOnlyList<string> sourceHeaders,
        IReadOnlyList<PredefinedHeader> targetHeaders,
        IReadOnlyList<IReadOnlyList<string>> sampleRows)
    {
        var targetKeys = targetHeaders.Select(header => header.Key).Append(IgnoredTargetKey).ToArray();
        var input = new
        {
            sourceHeaders,
            targetFields = targetHeaders.Select(header => new
            {
                header.Key,
                header.DisplayName,
                header.DataType,
                header.Aliases,
                header.Required
            }),
            sampleRows
        };

        return new
        {
            model,
            input = new object[]
            {
                new
                {
                    role = "system",
                    content = "You are an AI data import assistant. Map each spreadsheet source header to at most one predefined DTO field. Use __ignore when no target is a good semantic fit. Return JSON that exactly matches the schema."
                },
                new
                {
                    role = "user",
                    content = JsonSerializer.Serialize(input, JsonOptions)
                }
            },
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "header_mapping_suggestions",
                    strict = true,
                    schema = BuildResponseSchema(targetKeys)
                }
            },
            temperature = 0,
            max_output_tokens = 1200
        };
    }

    private static object BuildResponseSchema(IReadOnlyList<string> targetKeys)
    {
        return new Dictionary<string, object?>
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["properties"] = new Dictionary<string, object?>
            {
                ["mappings"] = new Dictionary<string, object?>
                {
                    ["type"] = "array",
                    ["items"] = new Dictionary<string, object?>
                    {
                        ["type"] = "object",
                        ["additionalProperties"] = false,
                        ["properties"] = new Dictionary<string, object?>
                        {
                            ["sourceHeader"] = new Dictionary<string, object?> { ["type"] = "string" },
                            ["targetKey"] = new Dictionary<string, object?> { ["type"] = "string", ["enum"] = targetKeys },
                            ["confidence"] = new Dictionary<string, object?> { ["type"] = "number" },
                            ["rationale"] = new Dictionary<string, object?> { ["type"] = "string" }
                        },
                        ["required"] = new[] { "sourceHeader", "targetKey", "confidence", "rationale" }
                    }
                }
            },
            ["required"] = new[] { "mappings" }
        };
    }

    private static string? ExtractOutputText(JsonElement root)
    {
        if (root.TryGetProperty("output_text", out var directOutputText)
            && directOutputText.ValueKind == JsonValueKind.String)
        {
            return directOutputText.GetString();
        }

        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var builder = new StringBuilder();

        foreach (var outputItem in output.EnumerateArray())
        {
            if (!outputItem.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var contentItem in content.EnumerateArray())
            {
                if (contentItem.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                {
                    builder.Append(text.GetString());
                }
            }
        }

        return builder.Length == 0 ? null : builder.ToString();
    }

    private sealed record OpenAiMappingEnvelope(IReadOnlyList<OpenAiMappingItem> Mappings);

    private sealed record OpenAiMappingItem(
        string SourceHeader,
        string TargetKey,
        double Confidence,
        string Rationale);
}

public sealed class FallbackHeaderMappingService
{
    public IReadOnlyList<MappingSuggestion> Suggest(
        IReadOnlyList<string> sourceHeaders,
        IReadOnlyList<PredefinedHeader> targetHeaders,
        IReadOnlyList<IReadOnlyList<string>> sampleRows)
    {
        var rankedSuggestions = sourceHeaders
            .Select((sourceHeader, index) => RankSourceHeader(sourceHeader, index, targetHeaders, sampleRows))
            .ToList();

        var selectedByTarget = rankedSuggestions
            .Where(suggestion => suggestion.TargetKey is not null)
            .GroupBy(suggestion => suggestion.TargetKey!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(suggestion => suggestion.Confidence).First(),
                StringComparer.OrdinalIgnoreCase);

        return rankedSuggestions
            .Select(suggestion =>
            {
                if (suggestion.TargetKey is null)
                {
                    return suggestion;
                }

                return selectedByTarget.TryGetValue(suggestion.TargetKey, out var selected)
                    && string.Equals(selected.SourceHeader, suggestion.SourceHeader, StringComparison.OrdinalIgnoreCase)
                    ? suggestion
                    : suggestion with
                    {
                        TargetKey = null,
                        Confidence = Math.Min(suggestion.Confidence, 0.35),
                        Reason = "No unique predefined header was confident enough."
                    };
            })
            .ToList();
    }

    private static MappingSuggestion RankSourceHeader(
        string sourceHeader,
        int sourceColumnIndex,
        IReadOnlyList<PredefinedHeader> targetHeaders,
        IReadOnlyList<IReadOnlyList<string>> sampleRows)
    {
        var sampleValues = sampleRows
            .Select(row => sourceColumnIndex < row.Count ? row[sourceColumnIndex] : string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Take(10)
            .ToList();

        var best = targetHeaders
            .Select(targetHeader => new
            {
                Header = targetHeader,
                Score = Score(sourceHeader, targetHeader, sampleValues)
            })
            .OrderByDescending(candidate => candidate.Score)
            .FirstOrDefault();

        if (best is null || best.Score < 0.38)
        {
            return new MappingSuggestion(sourceHeader, null, 0.25, "No predefined header was a confident match.", "Fallback");
        }

        return new MappingSuggestion(
            sourceHeader,
            best.Header.Key,
            Math.Round(Math.Clamp(best.Score, 0, 0.99), 2),
            $"Matched to {best.Header.DisplayName} using header text and sample values.",
            "Fallback");
    }

    private static double Score(string sourceHeader, PredefinedHeader targetHeader, IReadOnlyList<string> sampleValues)
    {
        var source = Normalize(sourceHeader);
        var candidates = new[] { targetHeader.Key, targetHeader.DisplayName }
            .Concat(targetHeader.Aliases)
            .Select(Normalize)
            .Where(candidate => candidate.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var textScore = candidates.Select(candidate => ScoreText(source, candidate)).DefaultIfEmpty(0).Max();
        var valueScore = ScoreValues(targetHeader.Key, sampleValues);

        return Math.Min(1, textScore + valueScore);
    }

    private static double ScoreText(string source, string target)
    {
        if (source.Length == 0 || target.Length == 0)
        {
            return 0;
        }

        if (source == target)
        {
            return 0.98;
        }

        var sourceTokens = Tokenize(source);
        var targetTokens = Tokenize(target);

        if (sourceTokens.SetEquals(targetTokens))
        {
            return 0.93;
        }

        var containsScore = source.Contains(target, StringComparison.OrdinalIgnoreCase)
            || target.Contains(source, StringComparison.OrdinalIgnoreCase)
            ? 0.78
            : 0;

        var jaccardScore = Jaccard(sourceTokens, targetTokens) * 0.82;
        var distanceScore = SimilarityRatio(source, target) * 0.72;

        return Math.Max(containsScore, Math.Max(jaccardScore, distanceScore));
    }

    private static double ScoreValues(string targetKey, IReadOnlyList<string> sampleValues)
    {
        if (sampleValues.Count == 0)
        {
            return 0;
        }

        var matchingValues = targetKey switch
        {
            "email" => sampleValues.Count(value => value.Contains('@', StringComparison.Ordinal)),
            "phoneNumber" => sampleValues.Count(value => Regex.IsMatch(value, @"^\+?[\d\s().-]{7,}$")),
            "signupDate" => sampleValues.Count(IsDate),
            "annualRevenue" => sampleValues.Count(IsDecimal),
            _ => 0
        };

        return matchingValues == 0 ? 0 : Math.Min(0.22, matchingValues / (double)sampleValues.Count * 0.22);
    }

    private static bool IsDate(string value)
    {
        return DateOnly.TryParse(value, CultureInfo.InvariantCulture, out _)
            || DateOnly.TryParse(value, CultureInfo.CurrentCulture, out _)
            || (double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var serial) && serial > 1 && serial < 60000);
    }

    private static bool IsDecimal(string value)
    {
        return decimal.TryParse(value, NumberStyles.Number | NumberStyles.Currency, CultureInfo.InvariantCulture, out _)
            || decimal.TryParse(value, NumberStyles.Number | NumberStyles.Currency, CultureInfo.CurrentCulture, out _);
    }

    private static string Normalize(string value)
    {
        var expanded = Regex.Replace(value, "([a-z])([A-Z])", "$1 $2");
        return Regex.Replace(expanded.ToLowerInvariant(), @"[^a-z0-9]+", " ").Trim();
    }

    private static HashSet<string> Tokenize(string value)
    {
        return value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static double Jaccard(HashSet<string> left, HashSet<string> right)
    {
        if (left.Count == 0 || right.Count == 0)
        {
            return 0;
        }

        var intersection = left.Intersect(right, StringComparer.OrdinalIgnoreCase).Count();
        var union = left.Union(right, StringComparer.OrdinalIgnoreCase).Count();
        return intersection / (double)union;
    }

    private static double SimilarityRatio(string left, string right)
    {
        var distance = LevenshteinDistance(left, right);
        return 1 - distance / (double)Math.Max(left.Length, right.Length);
    }

    private static int LevenshteinDistance(string left, string right)
    {
        var costs = new int[right.Length + 1];

        for (var j = 0; j <= right.Length; j++)
        {
            costs[j] = j;
        }

        for (var i = 1; i <= left.Length; i++)
        {
            costs[0] = i;
            var previousDiagonal = i - 1;

            for (var j = 1; j <= right.Length; j++)
            {
                var previousCost = costs[j];
                costs[j] = left[i - 1] == right[j - 1]
                    ? previousDiagonal
                    : Math.Min(Math.Min(costs[j - 1], costs[j]), previousDiagonal) + 1;
                previousDiagonal = previousCost;
            }
        }

        return costs[right.Length];
    }
}
