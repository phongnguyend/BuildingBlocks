namespace TemplateEngine;

/// <summary>Parses template tokens into an abstract syntax tree.</summary>
public static class TemplateParser
{
    public static IReadOnlyList<TemplateNode> Parse(IReadOnlyList<Token> tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        var root = new List<TemplateNode>();
        var parents = new Stack<List<TemplateNode>>();
        var openBlocks = new Stack<string>();
        var current = root;

        foreach (var token in tokens)
        {
            if (token.Type == TokenType.Text)
            {
                current.Add(new TextNode { Text = token.Value });
                continue;
            }

            var expression = token.Value;
            if (expression.StartsWith("if ", StringComparison.Ordinal))
            {
                var condition = expression[3..].Trim();
                EnsureNotEmpty(condition, "if condition");

                var node = new IfNode { Condition = condition };
                current.Add(node);
                parents.Push(current);
                openBlocks.Push("if");
                current = node.Children;
            }
            else if (expression.StartsWith("foreach ", StringComparison.Ordinal))
            {
                var content = expression["foreach ".Length..];
                var pieces = content.Split(" in ", 2, StringSplitOptions.TrimEntries);
                if (pieces.Length != 2 || pieces[0].Length == 0 || pieces[1].Length == 0 ||
                    pieces[0].Any(character => !(char.IsLetterOrDigit(character) || character == '_')) ||
                    !(char.IsLetter(pieces[0][0]) || pieces[0][0] == '_'))
                {
                    throw new TemplateSyntaxException(
                        "Invalid foreach expression. Expected 'foreach <variable> in <collection>'.");
                }

                var node = new ForEachNode
                {
                    VariableName = pieces[0],
                    CollectionExpression = pieces[1]
                };

                current.Add(node);
                parents.Push(current);
                openBlocks.Push("foreach");
                current = node.Children;
            }
            else if (expression.Equals("end", StringComparison.Ordinal))
            {
                if (parents.Count == 0)
                {
                    throw new TemplateSyntaxException("Unexpected 'end'.");
                }

                current = parents.Pop();
                openBlocks.Pop();
            }
            else
            {
                current.Add(new ExpressionNode { Expression = expression });
            }
        }

        if (parents.Count != 0)
        {
            throw new TemplateSyntaxException($"Unclosed '{openBlocks.Peek()}' template block.");
        }

        return root;
    }

    private static void EnsureNotEmpty(string value, string description)
    {
        if (value.Length == 0)
        {
            throw new TemplateSyntaxException($"The {description} cannot be empty.");
        }
    }
}
