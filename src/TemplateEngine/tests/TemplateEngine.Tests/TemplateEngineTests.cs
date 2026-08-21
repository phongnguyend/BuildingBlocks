using Engine = global::TemplateEngine.TemplateEngine;

namespace TemplateEngine.Tests;

public sealed class TemplateEngineTests
{
    private readonly Engine _engine = new();

    [Fact]
    public void Render_ResolvesRootAndModelPropertyPaths()
    {
        var result = _engine.Render("Hello {{ Name }} / {{ Model.Address.City }}", new
        {
            Name = "Phong",
            Address = new { City = "Bangkok" }
        });

        Assert.Equal("Hello Phong / Bangkok", result);
    }

    [Fact]
    public void Render_ResolvesArrayIndexesWithinNestedPropertyPaths()
    {
        var result = _engine.Render(
            "{{ Customers[0].Addresses[1].City }} / {{ Model.Customers[1].Name }}",
            new
            {
                Customers = new[]
                {
                    new
                    {
                        Name = "First",
                        Addresses = new[] { new { City = "Hanoi" }, new { City = "Bangkok" } }
                    },
                    new
                    {
                        Name = "Second",
                        Addresses = new[] { new { City = "Tokyo" }, new { City = "Seoul" } }
                    }
                }
            });

        Assert.Equal("Bangkok / Second", result);
    }

    [Fact]
    public void Render_SupportsConsecutiveIndexesAndLists()
    {
        var result = _engine.Render(
            "{{ Matrix[0][1] }} / {{ Names[1] }}",
            new
            {
                Matrix = new[] { new[] { 10, 20 } },
                Names = new List<string> { "A", "B" }
            });

        Assert.Equal("20 / B", result);
    }

    [Fact]
    public void Render_UsesEmptyTextForAnOutOfRangeIndex()
    {
        Assert.Equal("ab", _engine.Render("a{{ Items[5].Name }}b", new { Items = new[] { new { Name = "A" } } }));
    }

    [Theory]
    [InlineData("Items[]")]
    [InlineData("Items[-1]")]
    [InlineData("Items[abc]")]
    [InlineData("Items[0")]
    [InlineData("Items[0]Name")]
    public void Render_RejectsMalformedIndexes(string expression)
    {
        Assert.Throws<TemplateSyntaxException>(() => _engine.Render($"{{{{ {expression} }}}}", new { Items = Array.Empty<object>() }));
    }

    [Fact]
    public void Render_HtmlEncodesExpressionValues()
    {
        var result = _engine.Render("<p>{{ Value }}</p>", new { Value = "<script>alert('x')</script> & more" });

        Assert.Equal("<p>&lt;script&gt;alert(&#39;x&#39;)&lt;/script&gt; &amp; more</p>", result);
    }

    [Fact]
    public void Render_RendersTruthfulConditionAndSkipsFalseCondition()
    {
        var result = _engine.Render(
            "{{ if Active }}active{{ end }}{{ if Missing }}missing{{ end }}",
            new { Active = true });

        Assert.Equal("active", result);
    }

    [Fact]
    public void Render_IteratesCollectionsAndResolvesLoopProperties()
    {
        var result = _engine.Render(
            "{{ foreach product in Products }}{{ product.Name }}={{ product.Price }};{{ end }}",
            new
            {
                Products = new[]
                {
                    new { Name = "A", Price = 12.5m },
                    new { Name = "B", Price = 7m }
                }
            });

        Assert.Equal("A=12.5;B=7;", result);
    }

    [Fact]
    public void Render_SupportsNestedBlocksAndScopedVariables()
    {
        var result = _engine.Render(
            "{{ foreach group in Groups }}{{ group.Name }}:" +
            "{{ foreach item in group.Items }}{{ if item.Visible }}{{ group.Name }}-{{ item.Name }};{{ end }}{{ end }}{{ end }}",
            new
            {
                Groups = new[]
                {
                    new { Name = "G1", Items = new[] { new { Name = "A", Visible = true }, new { Name = "B", Visible = false } } },
                    new { Name = "G2", Items = new[] { new { Name = "C", Visible = true }, new { Name = "D", Visible = true } } }
                }
            });

        Assert.Equal("G1:G1-A;G2:G2-C;G2-D;", result);
    }

    [Fact]
    public void Render_RestoresShadowedLoopVariableAfterNestedLoop()
    {
        var result = _engine.Render(
            "{{ foreach item in Rows }}{{ item.Name }}[{{ foreach item in item.Values }}{{ item }}{{ end }}]{{ item.Name }};{{ end }}",
            new { Rows = new[] { new { Name = "row", Values = new[] { 1, 2 } } } });

        Assert.Equal("row[12]row;", result);
    }

    [Fact]
    public void Render_CanReadDictionaryModels()
    {
        var model = new Dictionary<string, object?>
        {
            ["Name"] = "Dictionary",
            ["Nested"] = new Dictionary<string, object?> { ["Value"] = 42 }
        };

        Assert.Equal("Dictionary:42", _engine.Render("{{ Name }}:{{ Nested.Value }}", model));
    }

    [Fact]
    public void Render_UsesEmptyTextForMissingOrNullValues()
    {
        Assert.Equal("ab", _engine.Render("a{{ Missing }}b{{ NullValue }}", new { NullValue = (string?)null }));
    }
}
