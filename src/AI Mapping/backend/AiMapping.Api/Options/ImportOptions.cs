namespace AiMapping.Api.Options;

public sealed class ImportOptions
{
    public const string SectionName = "Import";

    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;
}
