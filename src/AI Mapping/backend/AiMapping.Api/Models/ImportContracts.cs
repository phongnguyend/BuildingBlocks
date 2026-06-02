namespace AiMapping.Api.Models;

public sealed record PredefinedHeader(
    string Key,
    string DisplayName,
    string DataType,
    IReadOnlyList<string> Aliases,
    bool Required);

public sealed record MappingSuggestion(
    string SourceHeader,
    string? TargetKey,
    double Confidence,
    string Reason,
    string SuggestedBy);

public sealed record AnalyzeImportResponse(
    string ImportId,
    string FileName,
    int HeaderRowIndex,
    IReadOnlyList<string> SourceHeaders,
    IReadOnlyList<PredefinedHeader> TargetHeaders,
    IReadOnlyList<IReadOnlyList<string>> PreviewRows,
    IReadOnlyList<MappingSuggestion> SuggestedMappings);

public sealed record HeaderMappingSelection(
    string SourceHeader,
    string? TargetKey);

public sealed record CompleteImportRequest(
    string ImportId,
    IReadOnlyList<HeaderMappingSelection> Mappings);

public sealed record CompleteImportResponse(
    IReadOnlyList<CustomerImportDto> Items,
    int TotalRows,
    int MappedRows,
    int SkippedRows,
    IReadOnlyList<string> Warnings);
