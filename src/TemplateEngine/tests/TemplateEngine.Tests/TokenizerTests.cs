namespace TemplateEngine.Tests;

public sealed class TokenizerTests
{
    [Fact]
    public void Tokenize_SplitsTextAndTrimmedExpressions()
    {
        var tokens = Tokenizer.Tokenize("Hello {{ Name }}!");

        Assert.Equal(
            [
                new Token(TokenType.Text, "Hello "),
                new Token(TokenType.Expression, "Name"),
                new Token(TokenType.Text, "!")
            ],
            tokens);
    }

    [Fact]
    public void Tokenize_PreservesTemplateWithoutExpressions()
    {
        var token = Assert.Single(Tokenizer.Tokenize("plain text"));

        Assert.Equal(new Token(TokenType.Text, "plain text"), token);
    }

    [Fact]
    public void Tokenize_RejectsMissingClosingDelimiter()
    {
        var exception = Assert.Throws<TemplateSyntaxException>(() => Tokenizer.Tokenize("{{ Name"));

        Assert.Equal(0, exception.Position);
    }

    [Fact]
    public void Tokenize_RejectsEmptyExpressions()
    {
        Assert.Throws<TemplateSyntaxException>(() => Tokenizer.Tokenize("{{   }}"));
    }
}
