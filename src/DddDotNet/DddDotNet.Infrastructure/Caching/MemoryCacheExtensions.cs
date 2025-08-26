using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Caching;

public static class MemoryCacheExtensions
{
    private static readonly ConcurrentDictionary<object, SemaphoreSlim> _locks = new();

    public static T GetOrSet<T>(
        this IMemoryCache cache,
        object key,
        Func<T> factory,
        MemoryCacheEntryOptions? options = null)
    {
        if (cache.TryGetValue(key, out T value))
        {
            return value;
        }

        var myLock = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        try
        {
            myLock.Wait();

            // Double-check inside lock
            if (cache.TryGetValue(key, out value))
            {
                return value;
            }

            value = factory();
            cache.Set(key, value, options ?? new MemoryCacheEntryOptions());
            return value;
        }
        finally
        {
            myLock.Release();
            _locks.TryRemove(key, out _);
        }
    }

    public static async Task<T> GetOrSetAsync<T>(
        this IMemoryCache cache,
        object key,
        Func<Task<T>> factory,
        MemoryCacheEntryOptions? options = null)
    {
        if (cache.TryGetValue(key, out T value))
        {
            return value;
        }

        var myLock = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        try
        {
            await myLock.WaitAsync();

            if (cache.TryGetValue(key, out value))
            {
                return value;
            }

            value = await factory();
            cache.Set(key, value, options ?? new MemoryCacheEntryOptions());
            return value;
        }
        finally
        {
            myLock.Release();
            _locks.TryRemove(key, out _);
        }
    }
}