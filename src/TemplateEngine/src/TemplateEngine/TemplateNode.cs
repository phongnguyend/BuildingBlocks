using System.Collections;
using System.Globalization;
using System.Net;
using System.Text;

namespace TemplateEngine;

/// <summary>A node in a parsed template's abstract syntax tree.</summary>
public abstract class TemplateNode
{
    public abstract void Render(RenderContext context, StringBuilder output);
}

public sealed class TextNode : TemplateNode
{
    public required string Text { get; init; }

    public override void Render(RenderContext context, StringBuilder output) => output.Append(Text);
}

public sealed class ExpressionNode : TemplateNode
{
    public required string Expression { get; init; }

    public override void Render(RenderContext context, StringBuilder output)
    {
        var value = context.Resolve(Expression);
        if (value is not null)
        {
            output.Append(WebUtility.HtmlEncode(Convert.ToString(value, CultureInfo.InvariantCulture)));
        }
    }
}

public sealed class IfNode : TemplateNode
{
    public required string Condition { get; init; }

    public List<TemplateNode> Children { get; } = [];

    public override void Render(RenderContext context, StringBuilder output)
    {
        if (!IsTrue(context.Resolve(Condition)))
        {
            return;
        }

        foreach (var child in Children)
        {
            child.Render(context, output);
        }
    }

    private static bool IsTrue(object? value) => value switch
    {
        null => false,
        bool boolean => boolean,
        string text => text.Length > 0,
        sbyte number => number != 0,
        byte number => number != 0,
        short number => number != 0,
        ushort number => number != 0,
        int number => number != 0,
        uint number => number != 0,
        long number => number != 0,
        ulong number => number != 0,
        float number => number != 0,
        double number => number != 0,
        decimal number => number != 0,
        _ => true
    };
}

public sealed class ForEachNode : TemplateNode
{
    public required string VariableName { get; init; }

    public required string CollectionExpression { get; init; }

    public List<TemplateNode> Children { get; } = [];

    public override void Render(RenderContext context, StringBuilder output)
    {
        if (context.Resolve(CollectionExpression) is not IEnumerable items)
        {
            return;
        }

        foreach (var item in items)
        {
            var itemContext = context.CreateScope(VariableName, item);
            foreach (var child in Children)
            {
                child.Render(itemContext, output);
            }
        }
    }
}
