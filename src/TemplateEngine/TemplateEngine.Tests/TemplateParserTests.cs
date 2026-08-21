namespace TemplateEngine.Tests;


public sealed class TemplateParserTests
{
    [Theory]
    [InlineData("{{ end }}")]
    [InlineData("{{ if Active }}")]
    [InlineData("{{ foreach item Items }}{{ end }}")]
    [InlineData("{{ foreach 1item in Items }}{{ end }}")]
    public void Parse_RejectsInvalidBlockSyntax(string template)
    {
        var tokens = Tokenizer.Tokenize(template);

        Assert.Throws<TemplateSyntaxException>(() => TemplateParser.Parse(tokens));
    }

    [Fact]
    public void Parse_BuildsNestedAst()
    {
        var tokens = Tokenizer.Tokenize("{{ if Active }}{{ foreach item in Items }}{{ item }}{{ end }}{{ end }}");

        var root = TemplateParser.Parse(tokens);

        var ifNode = Assert.IsType<IfNode>(Assert.Single(root));
        var loop = Assert.IsType<ForEachNode>(Assert.Single(ifNode.Children));
        var expression = Assert.IsType<ExpressionNode>(Assert.Single(loop.Children));
        Assert.Equal("item", expression.Expression);
    }
}
