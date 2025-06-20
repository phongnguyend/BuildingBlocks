using System;
using System.Text;

namespace DddDotNet.CrossCuttingConcerns.Uris;

public static class UriPath
{
    public static string Combine(params string[] paths)
    {
        return Combine(paths.AsSpan());
    }

    public static string Combine(ReadOnlySpan<string> paths)
    {
        if (paths.Length == 0)
        {
            return string.Empty;
        }

        if (paths.Length == 1)
        {
            return paths[0];
        }

        var sb = new StringBuilder(paths[0].TrimEnd('/'));

        for (int i = 1; i < paths.Length; i++)
        {
            sb.Append('/').Append(paths[i].TrimStart('/'));
        }

        return sb.ToString();
    }
}
