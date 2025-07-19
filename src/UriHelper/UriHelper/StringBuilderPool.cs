using System.Collections.Concurrent;
using System.Text;

namespace UriHelper;

public class StringBuilderPool
{
    private readonly ConcurrentStack<StringBuilder> _stack = new ();

    public StringBuilder Get(int capacityHint)
    {
        if (!_stack.TryPop(out var result))
        {
            result = new StringBuilder(capacityHint);
        }

        return result;
    }

    public void Return(StringBuilder builder)
    {
        builder.Clear();
        _stack.Push(builder);
    }
}
