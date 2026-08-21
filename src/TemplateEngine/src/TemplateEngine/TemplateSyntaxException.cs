namespace TemplateEngine;

/// <summary>Represents invalid template syntax.</summary>
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
