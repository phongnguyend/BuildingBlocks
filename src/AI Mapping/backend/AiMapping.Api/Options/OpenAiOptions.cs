namespace AiMapping.Api.Options;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAI";

    public string Endpoint { get; set; } = "https://api.openai.com/v1";
    public string Model { get; set; } = "gpt-4o-mini";
    public string? ApiKey { get; set; }
}
