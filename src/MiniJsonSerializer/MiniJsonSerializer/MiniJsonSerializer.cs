using System.Collections;
using System.Reflection;
using System.Text;

namespace MiniJsonSerializer;

public static class MiniJsonSerializer
{
    public static string Serialize(object obj)
    {
        var sb = new StringBuilder();
        WriteValue(sb, obj, false, 0);
        return sb.ToString();
    }

    public static string Serialize(object obj, JsonSerializerOptions options)
    {
        var sb = new StringBuilder();
        bool indent = options?.WriteIndented ?? false;
        WriteValue(sb, obj, indent, 0);
        return sb.ToString();
    }

    static void WriteValue(StringBuilder sb, object value, bool indent, int depth)
    {
        if (value == null)
        {
            sb.Append("null");
            return;
        }

        switch (value)
        {
            case string s:
                WriteString(sb, s);
                break;

            case bool b:
                sb.Append(b ? "true" : "false");
                break;

            case int or long or float or double or decimal:
                sb.Append(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture));
                break;

            case IDictionary<string, object> dict:
                WriteDictionary(sb, dict, indent, depth);
                break;

            case IEnumerable enumerable when value is not string:
                WriteArray(sb, enumerable, indent, depth);
                break;

            default:
                WriteObject(sb, value, indent, depth);
                break;
        }
    }

    static void WriteString(StringBuilder sb, string value)
    {
        sb.Append('"');

        foreach (var c in value)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        sb.Append('"');
    }

    static void WriteArray(StringBuilder sb, IEnumerable array, bool indent, int depth)
    {
        sb.Append('[');

        bool first = true;

        foreach (var item in array)
        {
            if (!first)
                sb.Append(',');

            if (indent)
            {
                sb.AppendLine();
                sb.Append(new string(' ', (depth + 1) * 2));
            }

            WriteValue(sb, item, indent, depth + 1);
            first = false;
        }

        if (!first && indent)
        {
            sb.AppendLine();
            sb.Append(new string(' ', depth * 2));
        }

        sb.Append(']');
    }

    static void WriteDictionary(StringBuilder sb, IDictionary<string, object> dict, bool indent, int depth)
    {
        sb.Append('{');

        bool first = true;

        foreach (var kv in dict)
        {
            if (!first)
                sb.Append(',');

            if (indent)
            {
                sb.AppendLine();
                sb.Append(new string(' ', (depth + 1) * 2));
            }

            WriteString(sb, kv.Key);
            sb.Append(':');

            if (indent)
                sb.Append(' ');

            WriteValue(sb, kv.Value, indent, depth + 1);

            first = false;
        }

        if (!first && indent)
        {
            sb.AppendLine();
            sb.Append(new string(' ', depth * 2));
        }

        sb.Append('}');
    }

    static void WriteObject(StringBuilder sb, object obj, bool indent, int depth)
    {
        sb.Append('{');

        bool first = true;

        var props = obj.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in props)
        {
            if (!prop.CanRead)
                continue;

            var val = prop.GetValue(obj);

            if (!first)
                sb.Append(',');

            if (indent)
            {
                sb.AppendLine();
                sb.Append(new string(' ', (depth + 1) * 2));
            }

            WriteString(sb, prop.Name);
            sb.Append(':');

            if (indent)
                sb.Append(' ');

            WriteValue(sb, val, indent, depth + 1);

            first = false;
        }

        if (!first && indent)
        {
            sb.AppendLine();
            sb.Append(new string(' ', depth * 2));
        }

        sb.Append('}');
    }
}
