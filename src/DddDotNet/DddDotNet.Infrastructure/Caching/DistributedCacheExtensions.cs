using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Caching;

public static class DistributedCacheExtensions
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public static T GetOrSet<T>(
        this IDistributedCache cache,
        string key,
        Func<T> factory,
        DistributedCacheEntryOptions? options = null)
    {
        var cachedValue = cache.GetString(key);
        if (cachedValue != null)
        {
            return JsonSerializer.Deserialize<T>(cachedValue);
        }

        var myLock = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        try
        {
            myLock.Wait();

            // Double-check inside lock
            cachedValue = cache.GetString(key);
            if (cachedValue != null)
            {
                return JsonSerializer.Deserialize<T>(cachedValue);
            }

            var value = factory();
            var serializedValue = JsonSerializer.Serialize(value);
            cache.SetString(key, serializedValue, options ?? new DistributedCacheEntryOptions());
            return value;
        }
        finally
        {
            myLock.Release();
            _locks.TryRemove(key, out _);
        }
    }

    public static async Task<T> GetOrSetAsync<T>(
        this IDistributedCache cache,
        string key,
        Func<Task<T>> factory,
        DistributedCacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var cachedValue = await cache.GetStringAsync(key, cancellationToken);
        if (cachedValue != null)
        {
            return JsonSerializer.Deserialize<T>(cachedValue);
        }

        var myLock = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        try
        {
            await myLock.WaitAsync();

            // Double-check inside lock
            cachedValue = await cache.GetStringAsync(key, cancellationToken);
            if (cachedValue != null)
            {
                return JsonSerializer.Deserialize<T>(cachedValue);
            }

            var value = await factory();
            var serializedValue = JsonSerializer.Serialize(value);
            await cache.SetStringAsync(key, serializedValue, options ?? new DistributedCacheEntryOptions(), cancellationToken);
            return value;
        }
        finally
        {
            myLock.Release();
            _locks.TryRemove(key, out _);
        }
    }

    public static T GetOrSet<T>(
        this IDistributedCache cache,
        string key,
        Func<T> factory,
        JsonSerializerOptions? serializerOptions,
        DistributedCacheEntryOptions? cacheOptions = null)
    {
        var cachedValue = cache.GetString(key);
        if (cachedValue != null)
        {
            return JsonSerializer.Deserialize<T>(cachedValue, serializerOptions);
        }

        var myLock = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        try
        {
            myLock.Wait();

            // Double-check inside lock
            cachedValue = cache.GetString(key);
            if (cachedValue != null)
            {
                return JsonSerializer.Deserialize<T>(cachedValue, serializerOptions);
            }

            var value = factory();
            var serializedValue = JsonSerializer.Serialize(value, serializerOptions);
            cache.SetString(key, serializedValue, cacheOptions ?? new DistributedCacheEntryOptions());
            return value;
        }
        finally
        {
            myLock.Release();
            _locks.TryRemove(key, out _);
        }
    }

    public static async Task<T> GetOrSetAsync<T>(
        this IDistributedCache cache,
        string key,
        Func<Task<T>> factory,
        JsonSerializerOptions? serializerOptions,
        DistributedCacheEntryOptions? cacheOptions = null,
        CancellationToken cancellationToken = default)
    {
        var cachedValue = await cache.GetStringAsync(key, cancellationToken);
        if (cachedValue != null)
        {
            return JsonSerializer.Deserialize<T>(cachedValue, serializerOptions);
        }

        var myLock = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        try
        {
            await myLock.WaitAsync(cancellationToken);

            // Double-check inside lock
            cachedValue = await cache.GetStringAsync(key, cancellationToken);
            if (cachedValue != null)
            {
                return JsonSerializer.Deserialize<T>(cachedValue, serializerOptions);
            }

            var value = await factory();
            var serializedValue = JsonSerializer.Serialize(value, serializerOptions);
            await cache.SetStringAsync(key, serializedValue, cacheOptions ?? new DistributedCacheEntryOptions(), cancellationToken);
            return value;
        }
        finally
        {
            myLock.Release();
            _locks.TryRemove(key, out _);
        }
    }
}