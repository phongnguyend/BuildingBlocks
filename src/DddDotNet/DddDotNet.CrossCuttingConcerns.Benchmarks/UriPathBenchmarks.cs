using BenchmarkDotNet.Attributes;
using DddDotNet.CrossCuttingConcerns.Uris;

namespace DddDotNet.CrossCuttingConcerns.Benchmarks;

[MemoryDiagnoser]
public class UriPathBenchmarks
{

    private static readonly (string[] Inputs, string ExpectedOutput)[] _testCases = [
        // One path cases
        new (new[] { "https://stackoverflow.com/questions/372865" }, "https://stackoverflow.com/questions/372865"),
        new (new[] { "https://stackoverflow.com/questions/372865/" }, "https://stackoverflow.com/questions/372865/"),

        // Two paths cases without trailing slash
        new (new[] { "https://stackoverflow.com/questions/372865", "path-combine-for-urls" }, "https://stackoverflow.com/questions/372865/path-combine-for-urls"),
        new (new[] { "https://stackoverflow.com/questions/372865", "/path-combine-for-urls" }, "https://stackoverflow.com/questions/372865/path-combine-for-urls"),
        new (new[] { "https://stackoverflow.com/questions/372865/", "path-combine-for-urls" }, "https://stackoverflow.com/questions/372865/path-combine-for-urls"),
        new (new[] { "https://stackoverflow.com/questions/372865/", "/path-combine-for-urls" }, "https://stackoverflow.com/questions/372865/path-combine-for-urls"),

        // Two paths cases with trailing slash
        new (new[] { "https://stackoverflow.com/questions/372865", "path-combine-for-urls/" }, "https://stackoverflow.com/questions/372865/path-combine-for-urls/"),
        new (new[] { "https://stackoverflow.com/questions/372865", "/path-combine-for-urls/" }, "https://stackoverflow.com/questions/372865/path-combine-for-urls/"),
        new (new[] { "https://stackoverflow.com/questions/372865/", "path-combine-for-urls/" }, "https://stackoverflow.com/questions/372865/path-combine-for-urls/"),
        new (new[] { "https://stackoverflow.com/questions/372865/", "/path-combine-for-urls/" }, "https://stackoverflow.com/questions/372865/path-combine-for-urls/"),

        // Three paths cases without trailing slash
        new (new[] { "https://stackoverflow.com/questions/372865", "path-combine-for-urls", "xxx" }, "https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx"),
        new (new[] { "https://stackoverflow.com/questions/372865", "path-combine-for-urls", "/xxx" }, "https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx"),
        new (new[] { "https://stackoverflow.com/questions/372865", "/path-combine-for-urls", "xxx" }, "https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx"),
        new (new[] { "https://stackoverflow.com/questions/372865", "/path-combine-for-urls", "/xxx" }, "https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx"),
        new (new[] { "https://stackoverflow.com/questions/372865/", "path-combine-for-urls", "xxx" }, "https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx"),
        new (new[] { "https://stackoverflow.com/questions/372865/", "path-combine-for-urls", "/xxx" }, "https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx"),
        new (new[] { "https://stackoverflow.com/questions/372865/", "/path-combine-for-urls", "xxx" }, "https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx"),
        new (new[] { "https://stackoverflow.com/questions/372865/", "/path-combine-for-urls", "/xxx" }, "https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx"),

        // Three paths cases with trailing slash
        new (new[] { "https://stackoverflow.com/questions/372865", "path-combine-for-urls", "xxx/" }, "https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx/"),
        new (new[] { "https://stackoverflow.com/questions/372865", "path-combine-for-urls", "/xxx/" }, "https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx/"),
        new (new[] { "https://stackoverflow.com/questions/372865", "/path-combine-for-urls", "xxx/" }, "https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx/"),
        new (new[] { "https://stackoverflow.com/questions/372865", "/path-combine-for-urls", "/xxx/" }, "https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx/"),
        new (new[] { "https://stackoverflow.com/questions/372865/", "path-combine-for-urls", "xxx/" }, "https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx/"),
        new (new[] { "https://stackoverflow.com/questions/372865/", "path-combine-for-urls", "/xxx/" }, "https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx/"),
        new (new[] { "https://stackoverflow.com/questions/372865/", "/path-combine-for-urls", "xxx/" }, "https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx/"),
        new (new[] { "https://stackoverflow.com/questions/372865/", "/path-combine-for-urls", "/xxx/" }, "https://stackoverflow.com/questions/372865/path-combine-for-urls/xxx/")
    ];

    [Benchmark]
    public void CombineUsingString()
    {
        for (int i = 0; i < 1_000; i++)
        {
            foreach (var testCase in _testCases)
            {
                _ = UriPathInternal.CombineUsingString(testCase.Inputs);
            }
        }
    }

    [Benchmark]
    public void CombineUsingSpan()
    {
        for (int i = 0; i < 1_000; i++)
        {
            foreach (var testCase in _testCases)
            {
                _ = UriPathInternal.CombineUsingSpan(testCase.Inputs);
            }
        }
    }

    [Benchmark]
    public void CombineUsingSpanWithStringBuilderPool()
    {
        for (int i = 0; i < 1_000; i++)
        {
            foreach (var testCase in _testCases)
            {
                _ = UriPathInternal.CombineUsingSpanWithStringBuilderPool(testCase.Inputs);
            }
        }
    }
}