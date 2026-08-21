namespace TemplateEngine;

/// <summary>Thrown when template syntax is invalid.</summary>
public sealed class TemplateSyntaxException : Exception
{
    public TemplateSyntaxException(string message)
        : base(message)
    {
    }

    public TemplateSyntaxException(string message, int position)
        : base($"{message} Position: {position}.")
    {
        Position = position;
    }

    public int? Position { get; }
}
