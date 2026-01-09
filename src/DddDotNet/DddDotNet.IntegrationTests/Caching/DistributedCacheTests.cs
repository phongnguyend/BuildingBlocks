using DddDotNet.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DddDotNet.IntegrationTests.Caching;

public abstract class DistributedCacheTests : IDisposable
{
    protected IServiceProvider _serviceProvider;
    protected IDistributedCache _distributedCache;

    public DistributedCacheTests()
    {
    }

    [Fact]
    public async Task SetAsync_ShouldStoreValueInCosmosCache()
    {
        // Arrange
        var key = $"test-key-{Guid.NewGuid()}";
        var value = "test-value";
        var valueBytes = Encoding.UTF8.GetBytes(value);

        // Act
        await _distributedCache.SetAsync(key, valueBytes);

        // Assert
        var retrievedBytes = await _distributedCache.GetAsync(key);
        var retrievedValue = Encoding.UTF8.GetString(retrievedBytes);

        Assert.Equal(value, retrievedValue);
    }

    [Fact]
    public async Task GetAsync_WhenKeyDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var key = $"non-existent-key-{Guid.NewGuid()}";

        // Act
        var result = await _distributedCache.GetAsync(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_WithOptions_ShouldRespectExpiration()
    {
        // Arrange
        var key = $"expiring-key-{Guid.NewGuid()}";
        var value = "expiring-value";
        var valueBytes = Encoding.UTF8.GetBytes(value);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(2)
        };

        // Act
        await _distributedCache.SetAsync(key, valueBytes, options);

        // Verify value is initially there
        var initialResult = await _distributedCache.GetAsync(key);
        Assert.NotNull(initialResult);

        // Wait for expiration
        await Task.Delay(TimeSpan.FromSeconds(3));

        // Assert
        var expiredResult = await _distributedCache.GetAsync(key);
        Assert.Null(expiredResult);
    }

    [Fact]
    public async Task SetAsync_WithSlidingExpiration_ShouldExtendExpirationOnAccess()
    {
        // Arrange
        var key = $"sliding-key-{Guid.NewGuid()}";
        var value = "sliding-value";
        var valueBytes = Encoding.UTF8.GetBytes(value);
        var options = new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromSeconds(3)
        };

        // Act
        await _distributedCache.SetAsync(key, valueBytes, options);

        // Access the value multiple times with delays less than sliding expiration
        for (var i = 0; i < 3; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            var result = await _distributedCache.GetAsync(key);
            Assert.NotNull(result);
        }

        // Wait longer than sliding expiration without access
        await Task.Delay(TimeSpan.FromSeconds(4));

        // Assert
        var expiredResult = await _distributedCache.GetAsync(key);
        Assert.Null(expiredResult);
    }

    [Fact]
    public async Task RemoveAsync_ShouldDeleteValueFromCache()
    {
        // Arrange
        var key = $"remove-test-key-{Guid.NewGuid()}";
        var value = "remove-test-value";
        var valueBytes = Encoding.UTF8.GetBytes(value);

        await _distributedCache.SetAsync(key, valueBytes);

        // Verify value exists
        var existingValue = await _distributedCache.GetAsync(key);
        Assert.NotNull(existingValue);

        // Act
        await _distributedCache.RemoveAsync(key);

        // Assert
        var removedValue = await _distributedCache.GetAsync(key);
        Assert.Null(removedValue);
    }

    [Fact]
    public async Task RefreshAsync_ShouldExtendCacheExpiration()
    {
        // Arrange
        var key = $"refresh-test-key-{Guid.NewGuid()}";
        var value = "refresh-test-value";
        var valueBytes = Encoding.UTF8.GetBytes(value);
        var options = new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromSeconds(2)
        };

        await _distributedCache.SetAsync(key, valueBytes, options);

        // Act - Refresh after initial delay
        await Task.Delay(TimeSpan.FromSeconds(1));
        await _distributedCache.RefreshAsync(key);

        // Wait a bit more but less than total expiration
        await Task.Delay(TimeSpan.FromSeconds(1.5));

        // Assert - Value should still be there due to refresh
        var result = await _distributedCache.GetAsync(key);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task SetAsync_WithComplexObject_ShouldSerializeAndDeserializeCorrectly()
    {
        // Arrange
        var key = $"complex-object-key-{Guid.NewGuid()}";
        var complexObject = new TestObject
        {
            Id = Guid.NewGuid(),
            Name = "Test Object",
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            Tags = new[] { "tag1", "tag2", "tag3" }
        };

        var serialized = JsonSerializer.Serialize(complexObject);
        var valueBytes = Encoding.UTF8.GetBytes(serialized);

        // Act
        await _distributedCache.SetAsync(key, valueBytes);
        var retrievedBytes = await _distributedCache.GetAsync(key);

        // Assert
        Assert.NotNull(retrievedBytes);
        var retrievedSerialized = Encoding.UTF8.GetString(retrievedBytes);
        var retrievedObject = JsonSerializer.Deserialize<TestObject>(retrievedSerialized);

        Assert.Equal(complexObject.Id, retrievedObject.Id);
        Assert.Equal(complexObject.Name, retrievedObject.Name);
        Assert.Equal(complexObject.IsActive, retrievedObject.IsActive);
        Assert.Equal(complexObject.Tags, retrievedObject.Tags);
    }

    [Fact]
    public async Task Concurrent_SetAndGet_ShouldHandleMultipleOperations()
    {
        // Arrange
        var tasks = new Task[10];
        var keyPrefix = $"concurrent-test-{Guid.NewGuid()}";

        // Act - Perform concurrent set and get operations
        for (var i = 0; i < tasks.Length; i++)
        {
            var index = i;
            tasks[i] = Task.Run(async () =>
            {
                var key = $"{keyPrefix}-{index}";
                var value = $"value-{index}";
                var valueBytes = Encoding.UTF8.GetBytes(value);

                await _distributedCache.SetAsync(key, valueBytes);
                var retrievedBytes = await _distributedCache.GetAsync(key);
                var retrievedValue = Encoding.UTF8.GetString(retrievedBytes);

                Assert.Equal(value, retrievedValue);
            });
        }

        // Assert
        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task SetAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var key = $"cancellation-test-key-{Guid.NewGuid()}";
        var value = "cancellation-test-value";
        var valueBytes = Encoding.UTF8.GetBytes(value);
        var cancellationTokenSource = new CancellationTokenSource();

        // Act & Assert
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _distributedCache.SetAsync(key, valueBytes, cancellationTokenSource.Token));
    }

    [Fact]
    public async Task GetAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var key = $"get-cancellation-test-key-{Guid.NewGuid()}";
        var cancellationTokenSource = new CancellationTokenSource();

        // Act & Assert
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _distributedCache.GetAsync(key, cancellationTokenSource.Token));
    }

    [Fact]
    public async Task SetAsync_UpdateExistingKey_ShouldOverwriteValue()
    {
        // Arrange
        var key = $"update-test-key-{Guid.NewGuid()}";
        var originalValue = "original-value";
        var updatedValue = "updated-value";
        var originalBytes = Encoding.UTF8.GetBytes(originalValue);
        var updatedBytes = Encoding.UTF8.GetBytes(updatedValue);

        // Act
        await _distributedCache.SetAsync(key, originalBytes);
        var firstResult = await _distributedCache.GetAsync(key);

        await _distributedCache.SetAsync(key, updatedBytes);
        var secondResult = await _distributedCache.GetAsync(key);

        // Assert
        Assert.Equal(originalValue, Encoding.UTF8.GetString(firstResult));
        Assert.Equal(updatedValue, Encoding.UTF8.GetString(secondResult));
    }

    [Fact]
    public async Task SetAsync_WithLargeValue_ShouldHandleLargeData()
    {
        // Arrange
        var key = $"large-data-key-{Guid.NewGuid()}";
        var largeValue = new string('x', 10000); // 10KB string
        var valueBytes = Encoding.UTF8.GetBytes(largeValue);

        // Act
        await _distributedCache.SetAsync(key, valueBytes);
        var retrievedBytes = await _distributedCache.GetAsync(key);

        // Assert
        Assert.NotNull(retrievedBytes);
        var retrievedValue = Encoding.UTF8.GetString(retrievedBytes);
        Assert.Equal(largeValue, retrievedValue);
    }

    public void Dispose()
    {
        if (_serviceProvider is IDisposable disposableServiceProvider)
        {
            disposableServiceProvider.Dispose();
        }
    }

    private class TestObject
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public string[] Tags { get; set; }
    }
}

public class CosmosDistributedCacheTests : DistributedCacheTests, IDisposable
{
    public CosmosDistributedCacheTests()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets("09f024f8-e8d1-4b78-9ddd-da941692e8fa")
            .Build();

        var cachingOptions = new CachingOptions();
        config.GetSection("Caching").Bind(cachingOptions);

        var services = new ServiceCollection();
        services.AddCaches(cachingOptions);
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
        _distributedCache = _serviceProvider.GetRequiredService<IDistributedCache>();
    }
}

public class RedisDistributedCacheTests : DistributedCacheTests, IDisposable
{
    public RedisDistributedCacheTests()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets("09f024f8-e8d1-4b78-9ddd-da941692e8fa")
            .Build();

        var cachingOptions = new CachingOptions();
        config.GetSection("Caching").Bind(cachingOptions);

        cachingOptions.Distributed.Provider = "Redis";

        var services = new ServiceCollection();
        services.AddCaches(cachingOptions);
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
        _distributedCache = _serviceProvider.GetRequiredService<IDistributedCache>();
    }
}