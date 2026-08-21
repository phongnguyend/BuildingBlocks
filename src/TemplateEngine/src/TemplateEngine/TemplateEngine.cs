using System.Text;

namespace TemplateEngine;

/// <summary>Renders templates containing variables, conditions, and loops.</summary>
public sealed class TemplateEngine
{
    public string Render<T>(string template, T model)
    {
        ArgumentNullException.ThrowIfNull(template);

        var tokens = Tokenizer.Tokenize(template);
        var nodes = TemplateParser.Parse(tokens);
        var context = new RenderContext(model);
        var output = new StringBuilder(template.Length);

        foreach (var node in nodes)
        {
            node.Render(context, output);
        }

        return output.ToString();
    }
}
