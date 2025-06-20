using System.Collections.Concurrent;
using System.Text;

namespace DddDotNet.CrossCuttingConcerns.ObjectPools;

public class StringBuilderPool
{
    private readonly ConcurrentStack<StringBuilder> _stack = new ();

    public StringBuilder Get(int capacityHint)
    {
        if (!_stack.TryPop(out StringBuilder result))
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
