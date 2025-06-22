using DddDotNet.CrossCuttingConcerns.Uris;
using System.Collections.Concurrent;

namespace DddDotNet.CrossCuttingConcerns.UnitTests;

public class UriPathTests
{
    [Fact]
    public void Combine_OnePath_ReturnTheOriginal()
    {
        var url = UriPath.Combine("https://stackoverflow.com/questions/372865");
        Assert.Equal("https://stackoverflow.com/questions/372865", url);
    }

    [Fact]
    public void Combine_OnePath_ShouldNotRemoveTheSlashAtTheEnd()
    {
        var url = UriPath.Combine("https://stackoverflow.com/questions/372865/");
        Assert.Equal("https://stackoverflow.com/questions/372865/", url);
    }

    [Theory]
    [InlineData("https://stackoverflow.com/questions/372865", "path-combine-for-urls")]
    [InlineData("https://stackoverflow.com/questions/372865", "/path-combine-for-urls")]
    [InlineData("https://stackoverflow.com/questions/372865/", "path-combine-for-urls")]
    [InlineData("https://stackoverflow.com/questions/372865/", "/path-combine-for-urls")]
    public void Combine_TwoPaths_ReturnSameResult(string uri1, string uri2)
    {
        var url = UriPath.Combine(uri1, uri2);
        Assert.Equal("https://stackoverflow.com/questions/372865/path-combine-for-urls", url);
    }

    [Theory]
    [InlineData("https://stackoverflow.com/questions/372865", "path-combine-for-urls/")]
    [InlineData("https://stackoverflow.com/questions/372865", "/path-combine-for-urls/")]
    [InlineData("https://stackoverflow.com/questions/372865/", "path-combine-for-urls/")]
    [InlineData("https://stackoverflow.com/questions/372865/", "/path-combine-for-urls/")]
    public void Combine_TwoPaths_ShouldNotRemoveTheSlashAtTheEnd(string uri1, string uri2)
    {
        var url = UriPath.Combine(uri1, uri2);
        Assert.Equal("https://stackoverflow.com/questions/372865/path-combine-for-urls/", url);
    }

    [Theory]
    [InlineData("https://stackoverflow.com/questions/372865", "path-combine-for-urls", "xxx")]
    [InlineData("https://stackoverflow.com/questions/372865", "path-combine-for-urls", "/xxx")]
    [InlineData("https://stackoverflow.com/questions/372865", "/path-combine-for-urls", "xxx")]
    [InlineData("https://stackoverflow.com/questions/372865", "/path-combine-for-urls", "/xxx")]
    [InlineData("https://stackoverflow.com/questions/372865/", "path-combine-for-urls", "xxx")]
    [InlineData("https://stackoverflow.com/questions/372865/", "path-combine-for-urls", "/xxx")]
    [InlineData("https://stackoverflow.com/questions/372865/", "/path-combine-for-urls", "xxx")]
    [InlineData("https://stackoverflow.com/questions/372865/", "/path-combine-for-urls", "/xxx")]
    public void Combine_ThreePaths_ReturnSameResult(string uri1, string uri2, string uri3)
    {
        var url = UriPath.Combine(uri1, uri2, uri3);
        Assert.Equal("https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx", url);
    }

    [Theory]
    [InlineData("https://stackoverflow.com/questions/372865", "path-combine-for-urls", "xxx/")]
    [InlineData("https://stackoverflow.com/questions/372865", "path-combine-for-urls", "/xxx/")]
    [InlineData("https://stackoverflow.com/questions/372865", "/path-combine-for-urls", "xxx/")]
    [InlineData("https://stackoverflow.com/questions/372865", "/path-combine-for-urls", "/xxx/")]
    [InlineData("https://stackoverflow.com/questions/372865/", "path-combine-for-urls", "xxx/")]
    [InlineData("https://stackoverflow.com/questions/372865/", "path-combine-for-urls", "/xxx/")]
    [InlineData("https://stackoverflow.com/questions/372865/", "/path-combine-for-urls", "xxx/")]
    [InlineData("https://stackoverflow.com/questions/372865/", "/path-combine-for-urls", "/xxx/")]
    public void Combine_ThreePaths_ShouldNotRemoveTheSlashAtTheEnd(string uri1, string uri2, string uri3)
    {
        var url = UriPath.Combine(uri1, uri2, uri3);
        Assert.Equal("https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx/", url);
    }

    [Fact]
    public async Task Combine_MultiThreading()
    {
        // Collection of all test cases from the previous tests
        var testCases = new[]
        {
            // One path cases
            new { Inputs = new[] { "https://stackoverflow.com/questions/372865" }, ExpectedOutput = "https://stackoverflow.com/questions/372865" },
            new { Inputs = new[] { "https://stackoverflow.com/questions/372865/" }, ExpectedOutput = "https://stackoverflow.com/questions/372865/" },

            // Two paths cases without trailing slash
            new { Inputs = new[] { "https://stackoverflow.com/questions/372865", "path-combine-for-urls" }, ExpectedOutput = "https://stackoverflow.com/questions/372865/path-combine-for-urls" },
            new { Inputs = new[] { "https://stackoverflow.com/questions/372865", "/path-combine-for-urls" }, ExpectedOutput = "https://stackoverflow.com/questions/372865/path-combine-for-urls" },
            new { Inputs = new[] { "https://stackoverflow.com/questions/372865/", "path-combine-for-urls" }, ExpectedOutput = "https://stackoverflow.com/questions/372865/path-combine-for-urls" },
            new { Inputs = new[] { "https://stackoverflow.com/questions/372865/", "/path-combine-for-urls" }, ExpectedOutput = "https://stackoverflow.com/questions/372865/path-combine-for-urls" },

            // Two paths cases with trailing slash
            new { Inputs = new[] { "https://stackoverflow.com/questions/372865", "path-combine-for-urls/" }, ExpectedOutput = "https://stackoverflow.com/questions/372865/path-combine-for-urls/" },
            new { Inputs = new[] { "https://stackoverflow.com/questions/372865", "/path-combine-for-urls/" }, ExpectedOutput = "https://stackoverflow.com/questions/372865/path-combine-for-urls/" },
            new { Inputs = new[] { "https://stackoverflow.com/questions/372865/", "path-combine-for-urls/" }, ExpectedOutput = "https://stackoverflow.com/questions/372865/path-combine-for-urls/" },
            new { Inputs = new[] { "https://stackoverflow.com/questions/372865/", "/path-combine-for-urls/" }, ExpectedOutput = "https://stackoverflow.com/questions/372865/path-combine-for-urls/" },

            // Three paths cases without trailing slash
            new { Inputs = new[] { "https://stackoverflow.com/questions/372865", "path-combine-for-urls", "xxx" }, ExpectedOutput = "https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx" },
            new { Inputs = new[] { "https://stackoverflow.com/questions/372865", "path-combine-for-urls", "/xxx" }, ExpectedOutput = "https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx" },
            new { Inputs = new[] { "https://stackoverflow.com/questions/372865", "/path-combine-for-urls", "xxx" }, ExpectedOutput = "https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx" },
            new { Inputs = new[] { "https://stackoverflow.com/questions/372865", "/path-combine-for-urls", "/xxx" }, ExpectedOutput = "https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx" },
            new { Inputs = new[] { "https://stackoverflow.com/questions/372865/", "path-combine-for-urls", "xxx" }, ExpectedOutput = "https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx" },
            new { Inputs = new[] { "https://stackoverflow.com/questions/372865/", "path-combine-for-urls", "/xxx" }, ExpectedOutput = "https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx" },
            new { Inputs = new[] { "https://stackoverflow.com/questions/372865/", "/path-combine-for-urls", "xxx" }, ExpectedOutput = "https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx" },
            new { Inputs = new[] { "https://stackoverflow.com/questions/372865/", "/path-combine-for-urls", "/xxx" }, ExpectedOutput = "https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx" },

            // Three paths cases with trailing slash
            new { Inputs = new[] { "https://stackoverflow.com/questions/372865", "path-combine-for-urls", "xxx/" }, ExpectedOutput = "https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx/" },
            new { Inputs = new[] { "https://stackoverflow.com/questions/372865", "path-combine-for-urls", "/xxx/" }, ExpectedOutput = "https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx/" },
            new { Inputs = new[] { "https://stackoverflow.com/questions/372865", "/path-combine-for-urls", "xxx/" }, ExpectedOutput = "https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx/" },
            new { Inputs = new[] { "https://stackoverflow.com/questions/372865", "/path-combine-for-urls", "/xxx/" }, ExpectedOutput = "https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx/" },
            new { Inputs = new[] { "https://stackoverflow.com/questions/372865/", "path-combine-for-urls", "xxx/" }, ExpectedOutput = "https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx/" },
            new { Inputs = new[] { "https://stackoverflow.com/questions/372865/", "path-combine-for-urls", "/xxx/" }, ExpectedOutput = "https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx/" },
            new { Inputs = new[] { "https://stackoverflow.com/questions/372865/", "/path-combine-for-urls", "xxx/" }, ExpectedOutput = "https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx/" },
            new { Inputs = new[] { "https://stackoverflow.com/questions/372865/", "/path-combine-for-urls", "/xxx/" }, ExpectedOutput = "https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx/" }
        };

        // Run multiple tasks in parallel
        var results = new ConcurrentBag<bool>();
        var tasks = new Task[1000];

        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                // Each task will iterate through all test cases
                foreach (var testCase in testCases)
                {
                    var url = UriPath.Combine(testCase.Inputs);
                    var isValid = url == testCase.ExpectedOutput;
                    results.Add(isValid);
                }
            });
        }

        await Task.WhenAll(tasks);

        // Verify all results were successful
        Assert.All(results, Assert.True);
        Assert.Equal(testCases.Length * tasks.Length, results.Count);
    }
}