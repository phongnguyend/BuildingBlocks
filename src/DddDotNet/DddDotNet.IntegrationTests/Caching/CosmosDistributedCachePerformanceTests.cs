using DddDotNet.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace DddDotNet.IntegrationTests.Caching;

public class CosmosDistributedCachePerformanceTests : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDistributedCache _distributedCache;
    private readonly ITestOutputHelper _output;

    public CosmosDistributedCachePerformanceTests(ITestOutputHelper output)
    {
        _output = output;
        
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

    [Fact]
    public async Task Performance_BulkSetOperations_ShouldCompleteWithinReasonableTime()
    {
        // Arrange
        const int operationCount = 50;
        var keyPrefix = $"perf-set-{Guid.NewGuid()}";
        var stopwatch = Stopwatch.StartNew();

        // Act
        var tasks = new List<Task>();
        for (var i = 0; i < operationCount; i++)
        {
            var key = $"{keyPrefix}-{i}";
            var value = $"value-{i}";
            var valueBytes = Encoding.UTF8.GetBytes(value);
            
            tasks.Add(_distributedCache.SetAsync(key, valueBytes));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        _output.WriteLine($"Bulk set operations ({operationCount} items) completed in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average time per operation: {(double)stopwatch.ElapsedMilliseconds / operationCount:F2}ms");
        
        // Performance assertion - should complete within reasonable time (adjust as needed)
        Assert.True(stopwatch.ElapsedMilliseconds < 30000, $"Bulk operations took too long: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task Performance_BulkGetOperations_ShouldCompleteWithinReasonableTime()
    {
        // Arrange
        const int operationCount = 50;
        var keyPrefix = $"perf-get-{Guid.NewGuid()}";
        
        // Setup data first
        var setupTasks = new List<Task>();
        for (var i = 0; i < operationCount; i++)
        {
            var key = $"{keyPrefix}-{i}";
            var value = $"value-{i}";
            var valueBytes = Encoding.UTF8.GetBytes(value);
            setupTasks.Add(_distributedCache.SetAsync(key, valueBytes));
        }
        await Task.WhenAll(setupTasks);

        var stopwatch = Stopwatch.StartNew();

        // Act
        var getTasks = new List<Task<byte[]>>();
        for (var i = 0; i < operationCount; i++)
        {
            var key = $"{keyPrefix}-{i}";
            getTasks.Add(_distributedCache.GetAsync(key));
        }

        var results = await Task.WhenAll(getTasks);
        stopwatch.Stop();

        // Assert
        _output.WriteLine($"Bulk get operations ({operationCount} items) completed in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average time per operation: {(double)stopwatch.ElapsedMilliseconds / operationCount:F2}ms");
        
        Assert.All(results, result => Assert.NotNull(result));
        Assert.True(stopwatch.ElapsedMilliseconds < 15000, $"Bulk get operations took too long: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task Performance_MixedOperations_ShouldHandleConcurrentLoad()
    {
        // Arrange
        const int operationCount = 30;
        var keyPrefix = $"perf-mixed-{Guid.NewGuid()}";
        var stopwatch = Stopwatch.StartNew();

        // Act
        var tasks = new List<Task>();
        
        // Mix of set, get, and remove operations
        for (var i = 0; i < operationCount; i++)
        {
            var key = $"{keyPrefix}-{i}";
            var value = $"value-{i}";
            var valueBytes = Encoding.UTF8.GetBytes(value);
            
            // Set operation
            tasks.Add(_distributedCache.SetAsync(key, valueBytes));
            
            // Get operation (will be null initially, but tests concurrent access)
            tasks.Add(_distributedCache.GetAsync(key));
            
            // If even number, add a remove operation
            if (i % 2 == 0)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await Task.Delay(100); // Small delay to let set operation complete
                    await _distributedCache.RemoveAsync(key);
                }));
            }
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        _output.WriteLine($"Mixed operations ({tasks.Count} total operations) completed in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average time per operation: {(double)stopwatch.ElapsedMilliseconds / tasks.Count:F2}ms");
        
        Assert.True(stopwatch.ElapsedMilliseconds < 45000, $"Mixed operations took too long: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task Performance_LargeValueOperations_ShouldHandleEfficiently()
    {
        // Arrange
        const int operationCount = 10;
        var keyPrefix = $"perf-large-{Guid.NewGuid()}";
        var largeValue = new string('x', 50000); // 50KB value
        var largeValueBytes = Encoding.UTF8.GetBytes(largeValue);
        var stopwatch = Stopwatch.StartNew();

        // Act
        var tasks = new List<Task>();
        for (var i = 0; i < operationCount; i++)
        {
            var key = $"{keyPrefix}-{i}";
            tasks.Add(_distributedCache.SetAsync(key, largeValueBytes));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        _output.WriteLine($"Large value operations ({operationCount} items of {largeValueBytes.Length} bytes each) completed in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average time per operation: {(double)stopwatch.ElapsedMilliseconds / operationCount:F2}ms");
        _output.WriteLine($"Total data transferred: {largeValueBytes.Length * operationCount / 1024:F2} KB");
        
        // Verify the data was stored correctly
        var getResults = new List<Task<byte[]>>();
        for (var i = 0; i < operationCount; i++)
        {
            var key = $"{keyPrefix}-{i}";
            getResults.Add(_distributedCache.GetAsync(key));
        }

        var retrievedValues = await Task.WhenAll(getResults);
        Assert.All(retrievedValues, result => 
        {
            Assert.NotNull(result);
            Assert.Equal(largeValueBytes.Length, result.Length);
        });

        Assert.True(stopwatch.ElapsedMilliseconds < 60000, $"Large value operations took too long: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task Performance_SequentialVsParallel_ShouldShowPerformanceDifference()
    {
        // Arrange
        const int operationCount = 20;
        var keyPrefix = $"perf-comparison-{Guid.NewGuid()}";
        var value = "test-value";
        var valueBytes = Encoding.UTF8.GetBytes(value);

        // Sequential operations
        var sequentialStopwatch = Stopwatch.StartNew();
        for (var i = 0; i < operationCount; i++)
        {
            var key = $"{keyPrefix}-sequential-{i}";
            await _distributedCache.SetAsync(key, valueBytes);
        }
        sequentialStopwatch.Stop();

        // Parallel operations
        var parallelStopwatch = Stopwatch.StartNew();
        var parallelTasks = new List<Task>();
        for (var i = 0; i < operationCount; i++)
        {
            var key = $"{keyPrefix}-parallel-{i}";
            parallelTasks.Add(_distributedCache.SetAsync(key, valueBytes));
        }
        await Task.WhenAll(parallelTasks);
        parallelStopwatch.Stop();

        // Assert
        _output.WriteLine($"Sequential operations ({operationCount} items): {sequentialStopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Parallel operations ({operationCount} items): {parallelStopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Performance improvement: {(double)sequentialStopwatch.ElapsedMilliseconds / parallelStopwatch.ElapsedMilliseconds:F2}x");
        
        // Parallel should generally be faster (though not always guaranteed due to various factors)
        Assert.True(parallelStopwatch.ElapsedMilliseconds <= sequentialStopwatch.ElapsedMilliseconds * 1.2, 
            "Parallel operations should not be significantly slower than sequential");
    }

    [Fact]
    public async Task Stress_HighConcurrency_ShouldMaintainDataIntegrity()
    {
        // Arrange
        const int concurrentOperations = 100;
        const int uniqueKeys = 20;
        var keyPrefix = $"stress-test-{Guid.NewGuid()}";
        var results = new List<Task<string>>();

        // Act - Perform many concurrent operations on a limited set of keys
        var tasks = new List<Task>();
        for (var i = 0; i < concurrentOperations; i++)
        {
            var operationIndex = i;
            var keyIndex = i % uniqueKeys;
            var key = $"{keyPrefix}-{keyIndex}";
            var value = $"value-{operationIndex}";
            var valueBytes = Encoding.UTF8.GetBytes(value);

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await _distributedCache.SetAsync(key, valueBytes);
                    
                    // Small random delay to create more concurrency
                    await Task.Delay(Random.Shared.Next(1, 10));
                    
                    var retrieved = await _distributedCache.GetAsync(key);
                    if (retrieved != null)
                    {
                        var retrievedValue = Encoding.UTF8.GetString(retrieved);
                        // Value should be valid (one of the values we set)
                        Assert.StartsWith("value-", retrievedValue);
                    }
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"Operation {operationIndex} failed: {ex.Message}");
                    throw;
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Verify final state
        var finalResults = new List<Task<byte[]>>();
        for (var i = 0; i < uniqueKeys; i++)
        {
            var key = $"{keyPrefix}-{i}";
            finalResults.Add(_distributedCache.GetAsync(key));
        }

        var finalValues = await Task.WhenAll(finalResults);
        _output.WriteLine($"Stress test completed. Final state: {finalValues.Count(v => v != null)} out of {uniqueKeys} keys have values");
        
        // At least some keys should have values (exact number depends on timing of operations)
        Assert.True(finalValues.Count(v => v != null) > 0, "At least some keys should have values after stress test");
    }

    public void Dispose()
    {
        if (_serviceProvider is IDisposable disposableServiceProvider)
        {
            disposableServiceProvider.Dispose();
        }
    }
}