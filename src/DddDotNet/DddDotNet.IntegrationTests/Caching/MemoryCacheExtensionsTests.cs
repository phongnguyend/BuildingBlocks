using DddDotNet.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DddDotNet.IntegrationTests.Caching;

public class MemoryCacheExtensionsTests
{
    private IMemoryCache CreateMemoryCache()
    {
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var provider = services.BuildServiceProvider();
        return provider.GetService<IMemoryCache>();
    }

    [Fact]
    public void GetOrSet_WhenCacheEmpty_ShouldCallFactoryAndReturnValue()
    {
        // Arrange
        var cache = CreateMemoryCache();
        var key = "test-key";
        var expectedValue = "test-value";
        var factoryCalled = false;

        // Act
        var result = cache.GetOrSet(key, () =>
        {
            factoryCalled = true;
            return expectedValue;
        });

        // Assert
        Assert.Equal(expectedValue, result);
        Assert.True(factoryCalled);
    }

    [Fact]
    public void GetOrSet_WhenCacheHasValue_ShouldNotCallFactoryAndReturnCachedValue()
    {
        // Arrange
        var cache = CreateMemoryCache();
        var key = "test-key";
        var cachedValue = "cached-value";
        var factoryValue = "factory-value";
        
        cache.Set(key, cachedValue);
        var factoryCalled = false;

        // Act
        var result = cache.GetOrSet(key, () =>
        {
            factoryCalled = true;
            return factoryValue;
        });

        // Assert
        Assert.Equal(cachedValue, result);
        Assert.False(factoryCalled);
    }

    [Fact]
    public async Task GetOrSetAsync_WhenCacheEmpty_ShouldCallFactoryAndReturnValue()
    {
        // Arrange
        var cache = CreateMemoryCache();
        var key = "test-key";
        var expectedValue = "test-value";
        var factoryCalled = false;

        // Act
        var result = await cache.GetOrSetAsync(key, async () =>
        {
            factoryCalled = true;
            await Task.Delay(10);
            return expectedValue;
        });

        // Assert
        Assert.Equal(expectedValue, result);
        Assert.True(factoryCalled);
    }

    [Fact]
    public async Task GetOrSetAsync_WhenCacheHasValue_ShouldNotCallFactoryAndReturnCachedValue()
    {
        // Arrange
        var cache = CreateMemoryCache();
        var key = "test-key";
        var cachedValue = "cached-value";
        var factoryValue = "factory-value";
        
        cache.Set(key, cachedValue);
        var factoryCalled = false;

        // Act
        var result = await cache.GetOrSetAsync(key, async () =>
        {
            factoryCalled = true;
            await Task.Delay(10);
            return factoryValue;
        });

        // Assert
        Assert.Equal(cachedValue, result);
        Assert.False(factoryCalled);
    }

    [Fact]
    public void GetOrSet_MultipleThreadsSameKey_ShouldReturnSameObjectAndCallFactoryOnce()
    {
        // Arrange
        var cache = CreateMemoryCache();
        var key = "concurrent-key";
        var factoryCallCount = 0;
        var taskCount = 10;
        var results = new ConcurrentBag<object>();
        var barrier = new Barrier(taskCount);

        // Act
        var tasks = Enumerable.Range(0, taskCount).Select(_ => Task.Run(() =>
        {
            barrier.SignalAndWait(); // Ensure all threads start at the same time
            
            var result = cache.GetOrSet(key, () =>
            {
                Interlocked.Increment(ref factoryCallCount);
                Thread.Sleep(50); // Simulate work
                return new { Value = "test-object", ThreadId = Thread.CurrentThread.ManagedThreadId };
            });
            
            results.Add(result);
        })).ToArray();

        Task.WaitAll(tasks);

        // Assert
        Assert.Equal(1, factoryCallCount); // Factory should be called only once
        Assert.Equal(taskCount, results.Count);
        
        // All results should be the same object reference
        var firstResult = results.First();
        Assert.All(results, result => Assert.Same(firstResult, result));
    }

    [Fact]
    public async Task GetOrSetAsync_MultipleThreadsSameKey_ShouldReturnSameObjectAndCallFactoryOnce()
    {
        // Arrange
        var cache = CreateMemoryCache();
        var key = "concurrent-async-key";
        var factoryCallCount = 0;
        var taskCount = 10;
        var results = new ConcurrentBag<object>();
        var barrier = new Barrier(taskCount);

        // Act
        var tasks = Enumerable.Range(0, taskCount).Select(_ => Task.Run(async () =>
        {
            barrier.SignalAndWait(); // Ensure all threads start at the same time
            
            var result = await cache.GetOrSetAsync(key, async () =>
            {
                Interlocked.Increment(ref factoryCallCount);
                await Task.Delay(50); // Simulate async work
                return new { Value = "test-async-object", ThreadId = Thread.CurrentThread.ManagedThreadId };
            });
            
            results.Add(result);
        })).ToArray();

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(1, factoryCallCount); // Factory should be called only once
        Assert.Equal(taskCount, results.Count);
        
        // All results should be the same object reference
        var firstResult = results.First();
        Assert.All(results, result => Assert.Same(firstResult, result));
    }

    [Fact]
    public void GetOrSet_MultipleDifferentKeys_ShouldCallFactoryForEachKey()
    {
        // Arrange
        var cache = CreateMemoryCache();
        var taskCount = 10;
        var factoryCallCounts = new ConcurrentDictionary<string, int>();
        var results = new ConcurrentBag<(string Key, object Value)>();

        // Act
        var tasks = Enumerable.Range(0, taskCount).Select(i => Task.Run(() =>
        {
            var key = $"key-{i}";
            
            var result = cache.GetOrSet(key, () =>
            {
                factoryCallCounts.AddOrUpdate(key, 1, (k, v) => v + 1);
                Thread.Sleep(10); // Simulate work
                return new { Value = $"value-{i}", Key = key };
            });
            
            results.Add((key, result));
        })).ToArray();

        Task.WaitAll(tasks);

        // Assert
        Assert.Equal(taskCount, factoryCallCounts.Count);
        Assert.All(factoryCallCounts.Values, count => Assert.Equal(1, count));
        Assert.Equal(taskCount, results.Count);
        
        // Each key should have its own unique object
        var groupedResults = results.GroupBy(r => r.Key).ToList();
        Assert.Equal(taskCount, groupedResults.Count);
        Assert.All(groupedResults, group => Assert.Single(group));
    }

    [Fact]
    public async Task GetOrSetAsync_MultipleDifferentKeys_ShouldCallFactoryForEachKey()
    {
        // Arrange
        var cache = CreateMemoryCache();
        var taskCount = 10;
        var factoryCallCounts = new ConcurrentDictionary<string, int>();
        var results = new ConcurrentBag<(string Key, object Value)>();

        // Act
        var tasks = Enumerable.Range(0, taskCount).Select(i => Task.Run(async () =>
        {
            var key = $"async-key-{i}";
            
            var result = await cache.GetOrSetAsync(key, async () =>
            {
                factoryCallCounts.AddOrUpdate(key, 1, (k, v) => v + 1);
                await Task.Delay(10); // Simulate async work
                return new { Value = $"value-{i}", Key = key };
            });
            
            results.Add((key, result));
        })).ToArray();

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(taskCount, factoryCallCounts.Count);
        Assert.All(factoryCallCounts.Values, count => Assert.Equal(1, count));
        Assert.Equal(taskCount, results.Count);
        
        // Each key should have its own unique object
        var groupedResults = results.GroupBy(r => r.Key).ToList();
        Assert.Equal(taskCount, groupedResults.Count);
        Assert.All(groupedResults, group => Assert.Single(group));
    }

    [Fact]
    public void GetOrSet_WithCustomOptions_ShouldRespectCacheOptions()
    {
        // Arrange
        var cache = CreateMemoryCache();
        var key = "options-key";
        var value = "test-value";
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(100)
        };

        // Act
        var result1 = cache.GetOrSet(key, () => value, options);
        
        // Wait for expiration
        Thread.Sleep(150);
        
        var factoryCalled = false;
        var result2 = cache.GetOrSet(key, () =>
        {
            factoryCalled = true;
            return "new-value";
        });

        // Assert
        Assert.Equal(value, result1);
        Assert.Equal("new-value", result2);
        Assert.True(factoryCalled);
    }

    [Fact]
    public async Task GetOrSetAsync_WithCustomOptions_ShouldRespectCacheOptions()
    {
        // Arrange
        var cache = CreateMemoryCache();
        var key = "async-options-key";
        var value = "test-value";
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(100)
        };

        // Act
        var result1 = await cache.GetOrSetAsync(key, async () => 
        {
            await Task.Delay(10);
            return value;
        }, options);
        
        // Wait for expiration
        await Task.Delay(150);
        
        var factoryCalled = false;
        var result2 = await cache.GetOrSetAsync(key, async () =>
        {
            factoryCalled = true;
            await Task.Delay(10);
            return "new-value";
        });

        // Assert
        Assert.Equal(value, result1);
        Assert.Equal("new-value", result2);
        Assert.True(factoryCalled);
    }

    [Fact]
    public void GetOrSet_FactoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        var cache = CreateMemoryCache();
        var key = "exception-key";
        var expectedException = new InvalidOperationException("Factory failed");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            cache.GetOrSet<string>(key, () => throw expectedException);
        });

        Assert.Same(expectedException, exception);
        
        // Verify the failed result is not cached
        Assert.False(cache.TryGetValue(key, out _));
    }

    [Fact]
    public async Task GetOrSetAsync_FactoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        var cache = CreateMemoryCache();
        var key = "async-exception-key";
        var expectedException = new InvalidOperationException("Async factory failed");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await cache.GetOrSetAsync<string>(key, () => throw expectedException);
        });

        Assert.Same(expectedException, exception);
        
        // Verify the failed result is not cached
        Assert.False(cache.TryGetValue(key, out _));
    }

    [Fact]
    public void GetOrSet_StressTest_ShouldHandleHighConcurrency()
    {
        // Arrange
        var cache = CreateMemoryCache();
        var keyCount = 100;
        var threadsPerKey = 10;
        var totalTasks = keyCount * threadsPerKey;
        var factoryCallCounts = new ConcurrentDictionary<string, int>();
        var results = new ConcurrentBag<(string Key, object Value)>();
        var startSignal = new ManualResetEventSlim(false);

        // Act
        var tasks = Enumerable.Range(0, totalTasks).Select(i => Task.Run(() =>
        {
            startSignal.Wait(); // Wait for all tasks to be ready
            
            var keyIndex = i % keyCount;
            var key = $"stress-key-{keyIndex}";
            
            var result = cache.GetOrSet(key, () =>
            {
                factoryCallCounts.AddOrUpdate(key, 1, (k, v) => v + 1);
                Thread.Sleep(1); // Minimal work simulation
                return new { Value = $"value-{keyIndex}", CreatedAt = DateTime.UtcNow };
            });
            
            results.Add((key, result));
        })).ToArray();

        // Start all tasks simultaneously
        startSignal.Set();
        Task.WaitAll(tasks);

        // Assert
        Assert.Equal(keyCount, factoryCallCounts.Count);
        Assert.All(factoryCallCounts.Values, count => Assert.Equal(1, count)); // Each factory called only once
        Assert.Equal(totalTasks, results.Count);
        
        // Verify that all threads for the same key got the same object
        var groupedResults = results.GroupBy(r => r.Key).ToList();
        Assert.Equal(keyCount, groupedResults.Count);
        
        foreach (var group in groupedResults)
        {
            Assert.Equal(threadsPerKey, group.Count());
            var firstValue = group.First().Value;
            Assert.All(group, item => Assert.Same(firstValue, item.Value));
        }
    }

    [Fact]
    public async Task GetOrSetAsync_StressTest_ShouldHandleHighConcurrency()
    {
        // Arrange
        var cache = CreateMemoryCache();
        var keyCount = 100;
        var threadsPerKey = 10;
        var totalTasks = keyCount * threadsPerKey;
        var factoryCallCounts = new ConcurrentDictionary<string, int>();
        var results = new ConcurrentBag<(string Key, object Value)>();
        var startSignal = new ManualResetEventSlim(false);

        // Act
        var tasks = Enumerable.Range(0, totalTasks).Select(i => Task.Run(async () =>
        {
            startSignal.Wait(); // Wait for all tasks to be ready
            
            var keyIndex = i % keyCount;
            var key = $"async-stress-key-{keyIndex}";
            
            var result = await cache.GetOrSetAsync(key, async () =>
            {
                factoryCallCounts.AddOrUpdate(key, 1, (k, v) => v + 1);
                await Task.Delay(1); // Minimal async work simulation
                return new { Value = $"async-value-{keyIndex}", CreatedAt = DateTime.UtcNow, TaskId = Task.CurrentId };
            });
            
            results.Add((key, result));
        })).ToArray();

        // Start all tasks simultaneously
        startSignal.Set();
        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(keyCount, factoryCallCounts.Count);
        Assert.All(factoryCallCounts.Values, count => Assert.Equal(1, count)); // Each factory called only once
        Assert.Equal(totalTasks, results.Count);
        
        // Verify that all threads for the same key got the same object
        var groupedResults = results.GroupBy(r => r.Key).ToList();
        Assert.Equal(keyCount, groupedResults.Count);
        
        foreach (var group in groupedResults)
        {
            Assert.Equal(threadsPerKey, group.Count());
            var firstValue = group.First().Value;
            Assert.All(group, item => Assert.Same(firstValue, item.Value));
        }
    }
}