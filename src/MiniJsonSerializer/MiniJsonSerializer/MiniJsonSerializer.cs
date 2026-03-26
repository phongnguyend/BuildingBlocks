using System.Collections;
using System.Globalization;
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

    public static object? Deserialize(string json)
    {
        var reader = new JsonReader(json);
        var result = reader.ReadValue();
        reader.SkipWhitespace();
        if (reader.Position < reader.Length)
            throw new FormatException($"Unexpected trailing content at position {reader.Position}.");
        return result;
    }

    public static T? Deserialize<T>(string json)
    {
        var value = Deserialize(json);
        return (T?)ConvertTo(value, typeof(T));
    }

    static object? ConvertTo(object? value, Type targetType)
    {
        if (value == null)
        {
            if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                throw new InvalidCastException($"Cannot assign null to value type {targetType.Name}.");
            return null;
        }

        var underlyingType = Nullable.GetUnderlyingType(targetType);
        if (underlyingType != null)
            targetType = underlyingType;

        if (targetType == typeof(object))
            return value;

        if (targetType == typeof(string))
            return value is string s ? s : throw new InvalidCastException($"Cannot convert {value.GetType().Name} to String.");

        if (targetType == typeof(bool))
            return value is bool b ? b : throw new InvalidCastException($"Cannot convert {value.GetType().Name} to Boolean.");

        if (targetType == typeof(int))
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);

        if (targetType == typeof(long))
            return Convert.ToInt64(value, CultureInfo.InvariantCulture);

        if (targetType == typeof(float))
            return Convert.ToSingle(value, CultureInfo.InvariantCulture);

        if (targetType == typeof(double))
            return Convert.ToDouble(value, CultureInfo.InvariantCulture);

        if (targetType == typeof(decimal))
            return Convert.ToDecimal(value, CultureInfo.InvariantCulture);

        if (targetType == typeof(Dictionary<string, object>) || targetType == typeof(IDictionary<string, object>))
        {
            if (value is Dictionary<string, object?> dict)
            {
                var result = new Dictionary<string, object>();
                foreach (var kv in dict)
                    result[kv.Key] = kv.Value!;
                return result;
            }
            throw new InvalidCastException($"Cannot convert {value.GetType().Name} to Dictionary.");
        }

        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            if (value is Dictionary<string, object?> dict)
            {
                var keyType = targetType.GetGenericArguments()[0];
                var valType = targetType.GetGenericArguments()[1];
                var result = (IDictionary)Activator.CreateInstance(targetType)!;
                foreach (var kv in dict)
                    result[ConvertTo(kv.Key, keyType)!] = ConvertTo(kv.Value, valType);
                return result;
            }
            throw new InvalidCastException($"Cannot convert {value.GetType().Name} to {targetType.Name}.");
        }

        if (targetType.IsArray)
        {
            if (value is List<object?> list)
            {
                var elementType = targetType.GetElementType()!;
                var array = Array.CreateInstance(elementType, list.Count);
                for (int i = 0; i < list.Count; i++)
                    array.SetValue(ConvertTo(list[i], elementType), i);
                return array;
            }
            throw new InvalidCastException($"Cannot convert {value.GetType().Name} to {targetType.Name}.");
        }

        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
        {
            if (value is List<object?> list)
            {
                var elementType = targetType.GetGenericArguments()[0];
                var result = (IList)Activator.CreateInstance(targetType)!;
                foreach (var item in list)
                    result.Add(ConvertTo(item, elementType));
                return result;
            }
            throw new InvalidCastException($"Cannot convert {value.GetType().Name} to {targetType.Name}.");
        }

        if (targetType.IsClass && value is Dictionary<string, object?> objDict)
        {
            var obj = Activator.CreateInstance(targetType)!;
            var props = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in props)
            {
                if (!prop.CanWrite)
                    continue;
                if (objDict.TryGetValue(prop.Name, out var propValue))
                    prop.SetValue(obj, ConvertTo(propValue, prop.PropertyType));
            }
            return obj;
        }

        throw new InvalidCastException($"Cannot convert {value.GetType().Name} to {targetType.Name}.");
    }

    private ref struct JsonReader
    {
        private readonly ReadOnlySpan<char> _json;
        private int _position;

        public JsonReader(string json)
        {
            _json = json.AsSpan();
            _position = 0;
        }

        public int Position => _position;
        public int Length => _json.Length;

        public void SkipWhitespace()
        {
            while (_position < _json.Length && char.IsWhiteSpace(_json[_position]))
                _position++;
        }

        public object? ReadValue()
        {
            SkipWhitespace();

            if (_position >= _json.Length)
                throw new FormatException("Unexpected end of JSON.");

            var c = _json[_position];

            return c switch
            {
                '"' => ReadString(),
                '{' => ReadObject(),
                '[' => ReadArray(),
                't' or 'f' => ReadBool(),
                'n' => ReadNull(),
                _ when c == '-' || char.IsDigit(c) => ReadNumber(),
                _ => throw new FormatException($"Unexpected character '{c}' at position {_position}.")
            };
        }

        string ReadString()
        {
            Expect('"');
            var sb = new StringBuilder();

            while (_position < _json.Length)
            {
                var c = _json[_position++];

                if (c == '"')
                    return sb.ToString();

                if (c == '\\')
                {
                    if (_position >= _json.Length)
                        throw new FormatException("Unexpected end of JSON in string escape.");

                    var escaped = _json[_position++];
                    sb.Append(escaped switch
                    {
                        '"' => '"',
                        '\\' => '\\',
                        '/' => '/',
                        'n' => '\n',
                        'r' => '\r',
                        't' => '\t',
                        'b' => '\b',
                        'f' => '\f',
                        'u' => ReadUnicodeEscape(),
                        _ => throw new FormatException($"Invalid escape character '\\{escaped}' at position {_position - 1}.")
                    });
                }
                else
                {
                    sb.Append(c);
                }
            }

            throw new FormatException("Unterminated string.");
        }

        char ReadUnicodeEscape()
        {
            if (_position + 4 > _json.Length)
                throw new FormatException("Invalid unicode escape sequence.");

            var hex = _json.Slice(_position, 4);
            _position += 4;

            if (int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int codePoint))
                return (char)codePoint;

            throw new FormatException("Invalid unicode escape sequence.");
        }

        Dictionary<string, object?> ReadObject()
        {
            Expect('{');
            var dict = new Dictionary<string, object?>();
            SkipWhitespace();

            if (_position < _json.Length && _json[_position] == '}')
            {
                _position++;
                return dict;
            }

            while (true)
            {
                SkipWhitespace();
                var key = ReadString();
                SkipWhitespace();
                Expect(':');
                var value = ReadValue();
                dict[key] = value;

                SkipWhitespace();
                if (_position < _json.Length && _json[_position] == ',')
                {
                    _position++;
                    continue;
                }
                break;
            }

            SkipWhitespace();
            Expect('}');
            return dict;
        }

        List<object?> ReadArray()
        {
            Expect('[');
            var list = new List<object?>();
            SkipWhitespace();

            if (_position < _json.Length && _json[_position] == ']')
            {
                _position++;
                return list;
            }

            while (true)
            {
                var value = ReadValue();
                list.Add(value);

                SkipWhitespace();
                if (_position < _json.Length && _json[_position] == ',')
                {
                    _position++;
                    continue;
                }
                break;
            }

            SkipWhitespace();
            Expect(']');
            return list;
        }

        object ReadBool()
        {
            if (Match("true"))
                return true;
            if (Match("false"))
                return false;

            throw new FormatException($"Unexpected token at position {_position}.");
        }

        object? ReadNull()
        {
            if (Match("null"))
                return null;

            throw new FormatException($"Unexpected token at position {_position}.");
        }

        object ReadNumber()
        {
            int start = _position;

            if (_position < _json.Length && _json[_position] == '-')
                _position++;

            while (_position < _json.Length && char.IsDigit(_json[_position]))
                _position++;

            bool isFloatingPoint = false;

            if (_position < _json.Length && _json[_position] == '.')
            {
                isFloatingPoint = true;
                _position++;
                while (_position < _json.Length && char.IsDigit(_json[_position]))
                    _position++;
            }

            if (_position < _json.Length && (_json[_position] == 'e' || _json[_position] == 'E'))
            {
                isFloatingPoint = true;
                _position++;
                if (_position < _json.Length && (_json[_position] == '+' || _json[_position] == '-'))
                    _position++;
                while (_position < _json.Length && char.IsDigit(_json[_position]))
                    _position++;
            }

            var numberSpan = _json[start.._position];

            if (isFloatingPoint)
            {
                if (double.TryParse(numberSpan, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                    return d;

                throw new FormatException($"Invalid number at position {start}.");
            }
            else
            {
                if (long.TryParse(numberSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l))
                {
                    if (l is >= int.MinValue and <= int.MaxValue)
                        return (int)l;
                    return l;
                }

                throw new FormatException($"Invalid number at position {start}.");
            }
        }

        void Expect(char expected)
        {
            if (_position >= _json.Length || _json[_position] != expected)
                throw new FormatException($"Expected '{expected}' at position {_position}.");
            _position++;
        }

        bool Match(string expected)
        {
            if (_position + expected.Length > _json.Length)
                return false;

            for (int i = 0; i < expected.Length; i++)
            {
                if (_json[_position + i] != expected[i])
                    return false;
            }

            _position += expected.Length;
            return true;
        }
    }
}
