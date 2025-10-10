using DddDotNet.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DddDotNet.IntegrationTests.Caching;

public class DistributedCacheExtensionsTests
{
    private IDistributedCache CreateDistributedCache()
    {
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        var provider = services.BuildServiceProvider();
        return provider.GetService<IDistributedCache>();
    }

    [Fact]
    public void GetOrSet_WhenCacheEmpty_ShouldCallFactoryAndReturnValue()
    {
        // Arrange
        var cache = CreateDistributedCache();
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
        var cache = CreateDistributedCache();
        var key = "test-key";
        var cachedValue = "cached-value";
        var factoryValue = "factory-value";

        cache.SetString(key, JsonSerializer.Serialize(cachedValue));
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
        var cache = CreateDistributedCache();
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
        var cache = CreateDistributedCache();
        var key = "test-key";
        var cachedValue = "cached-value";
        var factoryValue = "factory-value";

        await cache.SetStringAsync(key, JsonSerializer.Serialize(cachedValue));
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
    public void GetOrSet_WithComplexObject_ShouldSerializeAndDeserializeCorrectly()
    {
        // Arrange
        var cache = CreateDistributedCache();
        var key = "complex-key";
        var expectedValue = new TestObject { Id = 123, Name = "Test", CreatedAt = DateTime.UtcNow };
        var factoryCalled = false;

        // Act
        var result = cache.GetOrSet(key, () =>
        {
            factoryCalled = true;
            return expectedValue;
        });

        // Assert
        Assert.Equal(expectedValue.Id, result.Id);
        Assert.Equal(expectedValue.Name, result.Name);
        Assert.Equal(expectedValue.CreatedAt.Date, result.CreatedAt.Date); // Compare dates due to JSON serialization precision
        Assert.True(factoryCalled);
    }

    [Fact]
    public async Task GetOrSetAsync_WithComplexObject_ShouldSerializeAndDeserializeCorrectly()
    {
        // Arrange
        var cache = CreateDistributedCache();
        var key = "complex-async-key";
        var expectedValue = new TestObject { Id = 456, Name = "AsyncTest", CreatedAt = DateTime.UtcNow };
        var factoryCalled = false;

        // Act
        var result = await cache.GetOrSetAsync(key, async () =>
        {
            factoryCalled = true;
            await Task.Delay(10);
            return expectedValue;
        });

        // Assert
        Assert.Equal(expectedValue.Id, result.Id);
        Assert.Equal(expectedValue.Name, result.Name);
        Assert.Equal(expectedValue.CreatedAt.Date, result.CreatedAt.Date);
        Assert.True(factoryCalled);
    }

    [Fact]
    public void GetOrSet_MultipleThreadsSameKey_ShouldCallFactoryOnceAndReturnSameValue()
    {
        // Arrange
        var cache = CreateDistributedCache();
        var key = "concurrent-key";
        var factoryCallCount = 0;
        var taskCount = 10;
        var results = new ConcurrentBag<TestObject>();
        var barrier = new Barrier(taskCount);

        // Act
        var tasks = Enumerable.Range(0, taskCount).Select(_ => Task.Run(() =>
        {
            barrier.SignalAndWait(); // Ensure all threads start at the same time

            var result = cache.GetOrSet(key, () =>
            {
                Interlocked.Increment(ref factoryCallCount);
                Thread.Sleep(50); // Simulate work
                return new TestObject { Id = 999, Name = "ConcurrentTest", CreatedAt = DateTime.UtcNow };
            });

            results.Add(result);
        })).ToArray();

        Task.WaitAll(tasks);

        // Assert
        Assert.Equal(1, factoryCallCount); // Factory should be called only once
        Assert.Equal(taskCount, results.Count);

        // All results should have the same values (since they're deserialized from the same JSON)
        var firstResult = results.First();
        Assert.All(results, result =>
        {
            Assert.Equal(firstResult.Id, result.Id);
            Assert.Equal(firstResult.Name, result.Name);
        });
    }

    [Fact]
    public async Task GetOrSetAsync_MultipleThreadsSameKey_ShouldCallFactoryOnceAndReturnSameValue()
    {
        // Arrange
        var cache = CreateDistributedCache();
        var key = "concurrent-async-key";
        var factoryCallCount = 0;
        var taskCount = 10;
        var results = new ConcurrentBag<TestObject>();
        var barrier = new Barrier(taskCount);

        // Act
        var tasks = Enumerable.Range(0, taskCount).Select(_ => Task.Run(async () =>
        {
            barrier.SignalAndWait(); // Ensure all threads start at the same time

            var result = await cache.GetOrSetAsync(key, async () =>
            {
                Interlocked.Increment(ref factoryCallCount);
                await Task.Delay(50); // Simulate async work
                return new TestObject { Id = 888, Name = "AsyncConcurrentTest", CreatedAt = DateTime.UtcNow };
            });

            results.Add(result);
        })).ToArray();

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(1, factoryCallCount); // Factory should be called only once
        Assert.Equal(taskCount, results.Count);

        // All results should have the same values
        var firstResult = results.First();
        Assert.All(results, result =>
        {
            Assert.Equal(firstResult.Id, result.Id);
            Assert.Equal(firstResult.Name, result.Name);
        });
    }

    [Fact]
    public void GetOrSet_WithCustomOptions_ShouldRespectCacheOptions()
    {
        // Arrange
        var cache = CreateDistributedCache();
        var key = "options-key";
        var value = "test-value";
        var options = new DistributedCacheEntryOptions
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
        var cache = CreateDistributedCache();
        var key = "async-options-key";
        var value = "test-value";
        var options = new DistributedCacheEntryOptions
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
    public void GetOrSet_WithCustomSerializerOptions_ShouldUseCustomOptions()
    {
        // Arrange
        var cache = CreateDistributedCache();
        var key = "serializer-options-key";
        var value = new TestObject { Id = 123, Name = "Test", CreatedAt = DateTime.UtcNow };
        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Act
        var result = cache.GetOrSet(key, () => value, serializerOptions);

        // Verify the object was cached with camelCase serialization
        var cachedJson = cache.GetString(key);
        Assert.Contains("\"id\":", cachedJson); // Should be camelCase
        Assert.Contains("\"name\":", cachedJson);

        // Assert
        Assert.Equal(value.Id, result.Id);
        Assert.Equal(value.Name, result.Name);
    }

    [Fact]
    public async Task GetOrSetAsync_WithCustomSerializerOptions_ShouldUseCustomOptions()
    {
        // Arrange
        var cache = CreateDistributedCache();
        var key = "async-serializer-options-key";
        var value = new TestObject { Id = 456, Name = "AsyncTest", CreatedAt = DateTime.UtcNow };
        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Act
        var result = await cache.GetOrSetAsync(key, async () =>
        {
            await Task.Delay(10);
            return value;
        }, serializerOptions);

        // Verify the object was cached with camelCase serialization
        var cachedJson = await cache.GetStringAsync(key);
        Assert.Contains("\"id\":", cachedJson); // Should be camelCase
        Assert.Contains("\"name\":", cachedJson);

        // Assert
        Assert.Equal(value.Id, result.Id);
        Assert.Equal(value.Name, result.Name);
    }

    [Fact]
    public void GetOrSet_FactoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        var cache = CreateDistributedCache();
        var key = "exception-key";
        var expectedException = new InvalidOperationException("Factory failed");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            cache.GetOrSet<string>(key, () => throw expectedException);
        });

        Assert.Same(expectedException, exception);

        // Verify the failed result is not cached
        var cachedValue = cache.GetString(key);
        Assert.Null(cachedValue);
    }

    [Fact]
    public async Task GetOrSetAsync_FactoryThrowsException_ShouldPropagateException()
    {
        // Arrange
        var cache = CreateDistributedCache();
        var key = "async-exception-key";
        var expectedException = new InvalidOperationException("Async factory failed");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await cache.GetOrSetAsync<string>(key, () => throw expectedException);
        });

        Assert.Same(expectedException, exception);

        // Verify the failed result is not cached
        var cachedValue = await cache.GetStringAsync(key);
        Assert.Null(cachedValue);
    }

    private class TestObject
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}