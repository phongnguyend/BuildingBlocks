using System.Collections;
using System.Globalization;
using System.Reflection;

namespace TemplateEngine;

/// <summary>Provides model and scoped-variable lookup while rendering.</summary>
public sealed class RenderContext
{
    private readonly object? _model;
    private readonly RenderContext? _parent;
    private readonly IReadOnlyDictionary<string, object?> _variables;

    public RenderContext(object? model)
    {
        _model = model;
        _variables = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["Model"] = model
        };
    }

    private RenderContext(RenderContext parent, string name, object? value)
    {
        _model = parent._model;
        _parent = parent;
        _variables = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [name] = value
        };
    }

    /// <summary>
    /// Resolves a variable or model path such as <c>user.Address.City</c> or
    /// <c>orders[0].Lines[1].Name</c>.
    /// </summary>
    public object? Resolve(string expression)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);

        var path = expression.AsSpan().Trim();
        var position = 0;
        var isFirstMember = true;
        object? current = null;

        while (position < path.Length)
        {
            var memberStart = position;
            while (position < path.Length && path[position] is not ('.' or '['))
            {
                position++;
            }

            var member = path[memberStart..position].Trim();
            if (member.IsEmpty)
            {
                throw InvalidPath(expression);
            }

            var memberName = member.ToString();
            if (isFirstMember)
            {
                if (!TryGetVariable(memberName, out current))
                {
                    current = ResolveMember(_model, memberName);
                }

                isFirstMember = false;
            }
            else
            {
                current = ResolveMember(current, memberName);
            }

            SkipWhitespace(path, ref position);
            while (position < path.Length && path[position] == '[')
            {
                current = ResolveIndex(current, ParseIndex(path, ref position, expression));
                SkipWhitespace(path, ref position);
            }

            if (position == path.Length)
            {
                return current;
            }

            if (path[position] != '.')
            {
                throw InvalidPath(expression);
            }

            position++;
            SkipWhitespace(path, ref position);
            if (position == path.Length)
            {
                throw InvalidPath(expression);
            }
        }

        return current;
    }

    internal RenderContext CreateScope(string name, object? value) => new(this, name, value);

    private bool TryGetVariable(string name, out object? value)
    {
        if (_variables.TryGetValue(name, out value))
        {
            return true;
        }

        if (_parent is not null)
        {
            return _parent.TryGetVariable(name, out value);
        }

        value = null;
        return false;
    }

    private static object? ResolveMember(object? instance, string member)
    {
        if (instance is null)
        {
            return null;
        }

        if (instance is IReadOnlyDictionary<string, object?> readOnlyDictionary &&
            readOnlyDictionary.TryGetValue(member, out var readOnlyValue))
        {
            return readOnlyValue;
        }

        if (instance is IDictionary dictionary && dictionary.Contains(member))
        {
            return dictionary[member];
        }

        var property = instance.GetType().GetProperty(
            member,
            BindingFlags.Instance | BindingFlags.Public);

        return property is null || property.GetIndexParameters().Length != 0
            ? null
            : property.GetValue(instance);
    }

    private static object? ResolveIndex(object? instance, int index)
    {
        if (instance is null || index < 0)
        {
            return null;
        }

        if (instance is Array array)
        {
            return array.Rank == 1 &&
                   index >= array.GetLowerBound(0) &&
                   index <= array.GetUpperBound(0)
                ? array.GetValue(index)
                : null;
        }

        if (instance is IList list)
        {
            return index < list.Count ? list[index] : null;
        }

        // IReadOnlyList<T> does not necessarily implement the non-generic IList
        // interface, so invoke that interface without depending on its element type.
        var type = instance.GetType();
        var readOnlyList = type.GetInterfaces().FirstOrDefault(candidate =>
            candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IReadOnlyList<>));
        if (readOnlyList is null)
        {
            return null;
        }

        var elementType = readOnlyList.GetGenericArguments()[0];
        var readOnlyCollection = typeof(IReadOnlyCollection<>).MakeGenericType(elementType);
        var count = (int)readOnlyCollection.GetProperty("Count")!.GetValue(instance)!;

        return index < count
            ? readOnlyList.GetProperty("Item")!.GetValue(instance, [index])
            : null;
    }

    private static int ParseIndex(ReadOnlySpan<char> path, ref int position, string expression)
    {
        position++; // Opening '['.
        SkipWhitespace(path, ref position);

        var indexStart = position;
        while (position < path.Length && char.IsAsciiDigit(path[position]))
        {
            position++;
        }

        if (indexStart == position ||
            !int.TryParse(path[indexStart..position], NumberStyles.None, CultureInfo.InvariantCulture, out var index))
        {
            throw InvalidPath(expression);
        }

        SkipWhitespace(path, ref position);
        if (position >= path.Length || path[position] != ']')
        {
            throw InvalidPath(expression);
        }

        position++;
        return index;
    }

    private static void SkipWhitespace(ReadOnlySpan<char> path, ref int position)
    {
        while (position < path.Length && char.IsWhiteSpace(path[position]))
        {
            position++;
        }
    }

    private static TemplateSyntaxException InvalidPath(string expression) =>
        new($"Invalid property path '{expression}'.");
}
