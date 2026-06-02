namespace AiMapping.Api.Services;

internal sealed record ParsedImport(
    string FileName,
    IReadOnlyList<IReadOnlyList<string>> Rows,
    int HeaderRowIndex,
    IReadOnlyList<string> Headers);
