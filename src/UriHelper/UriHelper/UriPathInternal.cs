using System.Text;

namespace UriHelper;

public class UriPathInternal
{
    private static readonly StringBuilderPool _stringBuilderPool = new StringBuilderPool();

    public static string CombineUsingString(ReadOnlySpan<string> paths)
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

    public static string CombineUsingSpan(ReadOnlySpan<string> paths)
    {
        if (paths.Length == 0)
        {
            return string.Empty;
        }

        if (paths.Length == 1)
        {
            return paths[0];
        }

        var sb = new StringBuilder();
        sb.Append(paths[0].AsSpan().TrimEnd('/'));

        for (int i = 1; i < paths.Length; i++)
        {
            sb.Append('/').Append(paths[i].AsSpan().TrimStart('/'));
        }

        return sb.ToString();
    }

    public static string CombineUsingSpanWithStringBuilderPool(ReadOnlySpan<string> paths)
    {
        if (paths.Length == 0)
        {
            return string.Empty;
        }

        if (paths.Length == 1)
        {
            return paths[0];
        }

        var sb = _stringBuilderPool.Get(100);

        try
        {
            sb.Append(paths[0].AsSpan().TrimEnd('/'));

            for (int i = 1; i < paths.Length; i++)
            {
                sb.Append('/').Append(paths[i].AsSpan().TrimStart('/'));
            }

            return sb.ToString();
        }
        finally
        {
            _stringBuilderPool.Return(sb);
        }
    }
}
