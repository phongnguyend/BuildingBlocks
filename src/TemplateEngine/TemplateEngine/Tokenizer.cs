namespace TemplateEngine;

/// <summary>Tokenizes template text into literal text and expressions.</summary>
public static class Tokenizer
{
    public static IReadOnlyList<Token> Tokenize(string template)
    {
        ArgumentNullException.ThrowIfNull(template);

        var tokens = new List<Token>();
        var position = 0;

        while (position < template.Length)
        {
            var start = template.IndexOf("{{", position, StringComparison.Ordinal);

            if (start < 0)
            {
                tokens.Add(new Token(TokenType.Text, template[position..]));
                break;
            }

            if (start > position)
            {
                tokens.Add(new Token(TokenType.Text, template[position..start]));
            }

            var end = template.IndexOf("}}", start + 2, StringComparison.Ordinal);
            if (end < 0)
            {
                throw new TemplateSyntaxException("Missing closing '}}'.", start);
            }

            var expression = template[(start + 2)..end].Trim();
            if (expression.Length == 0)
            {
                throw new TemplateSyntaxException("Template expressions cannot be empty.", start);
            }

            tokens.Add(new Token(TokenType.Expression, expression));
            position = end + 2;
        }

        return tokens;
    }
}
