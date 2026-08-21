namespace TemplateEngine;

/// <summary>Identifies the kind of content found in a template.</summary>
public enum TokenType
{
    Text,
    Expression
}

/// <summary>A lexical token produced from template text.</summary>
public sealed record Token(TokenType Type, string Value);
