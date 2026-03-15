using System.Collections;
using System.Reflection;
using System.Text;

namespace MiniJsonSerializer;

public static class MiniJsonSerializer
{
    public static string Serialize(object obj)
    {
        var sb = new StringBuilder();
        WriteValue(sb, obj);
        return sb.ToString();
    }

    static void WriteValue(StringBuilder sb, object value)
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
                WriteDictionary(sb, dict);
                break;

            case IEnumerable enumerable when value is not string:
                WriteArray(sb, enumerable);
                break;

            default:
                WriteObject(sb, value);
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

    static void WriteArray(StringBuilder sb, IEnumerable array)
    {
        sb.Append('[');

        bool first = true;

        foreach (var item in array)
        {
            if (!first)
                sb.Append(',');

            WriteValue(sb, item);
            first = false;
        }

        sb.Append(']');
    }

    static void WriteDictionary(StringBuilder sb, IDictionary<string, object> dict)
    {
        sb.Append('{');

        bool first = true;

        foreach (var kv in dict)
        {
            if (!first)
                sb.Append(',');

            WriteString(sb, kv.Key);
            sb.Append(':');
            WriteValue(sb, kv.Value);

            first = false;
        }

        sb.Append('}');
    }

    static void WriteObject(StringBuilder sb, object obj)
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

            WriteString(sb, prop.Name);
            sb.Append(':');

            WriteValue(sb, val);

            first = false;
        }

        sb.Append('}');
    }
}