using System;

namespace DddDotNet.CrossCuttingConcerns.Uris;

public static class UriPath
{
    public static string Combine(params string[] paths)
    {
        return Combine(paths.AsSpan());
    }

    public static string Combine(ReadOnlySpan<string> paths)
    {
        return UriPathInternal.CombineUsingSpanWithStringBuilderPool(paths);
    }
}
