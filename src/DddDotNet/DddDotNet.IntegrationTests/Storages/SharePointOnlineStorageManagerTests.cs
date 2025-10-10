using DddDotNet.Infrastructure.Storages;
using DddDotNet.Infrastructure.Storages.SharePointOnline;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UriHelper;
using Xunit;

namespace DddDotNet.IntegrationTests.Storages;

public class SharePointOnlineStorageManagerTests
{
    SharePointOnlineOptions _options = new SharePointOnlineOptions();
    IMemoryCache _memoryCache;

    public SharePointOnlineStorageManagerTests()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets("09f024f8-e8d1-4b78-9ddd-da941692e8fa")
            .Build();

        config.GetSection("Storage:SharePointOnline").Bind(_options);

        // Create IMemoryCache instance using DI container
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var provider = services.BuildServiceProvider();
        _memoryCache = provider.GetService<IMemoryCache>();
    }

    [Fact]
    public async Task CreateAsync_Success()
    {
        var sharePointOnlineStorageManager = new SharePointOnlineStorageManager(_options, _memoryCache);

        var fileEntry = new FileEntry
        {
            FileLocation = UriPath.Combine(DateTime.Now.ToString("yyyy/MM/dd"), Guid.NewGuid().ToString())
        };

        var fileStream = new MemoryStream(Encoding.UTF8.GetBytes("Test"));

        await sharePointOnlineStorageManager.CreateAsync(fileEntry, fileStream);

        var content1 = Encoding.UTF8.GetString(await sharePointOnlineStorageManager.ReadAsync(fileEntry));

        fileStream = new MemoryStream(Encoding.UTF8.GetBytes("Test2"));

        await sharePointOnlineStorageManager.CreateAsync(fileEntry, fileStream);

        var content2 = Encoding.UTF8.GetString(await sharePointOnlineStorageManager.ReadAsync(fileEntry));

        await sharePointOnlineStorageManager.ArchiveAsync(fileEntry);

        await sharePointOnlineStorageManager.UnArchiveAsync(fileEntry);

        var path = Path.GetTempFileName();
        await sharePointOnlineStorageManager.DownloadAsync(fileEntry, path);
        var content3 = File.ReadAllText(path);
        File.Delete(path);

        path = Path.GetTempFileName();
        using (var tempFileStream = File.OpenWrite(path))
        {
            await sharePointOnlineStorageManager.DownloadAsync(fileEntry, tempFileStream);
        }
        var content4 = File.ReadAllText(path);
        File.Delete(path);

        await sharePointOnlineStorageManager.DeleteAsync(fileEntry);

        Assert.Equal("Test", content1);
        Assert.Equal("Test2", content2);
        Assert.Equal("Test2", content3);
        Assert.Equal("Test2", content4);
    }

    [Fact]
    public async Task HealthCheck_Healthy()
    {
        var healthCheck = new SharePointOnlineHealthCheck(_options, _memoryCache);
        var checkResult = await healthCheck.CheckHealthAsync(null);
        Assert.Equal(HealthStatus.Healthy, checkResult.Status);
    }

    [Fact]
    public async Task HealthCheck_Degraded()
    {
        _options.DocumentLibraryName = "NonExistentLibrary";
        var healthCheck = new SharePointOnlineHealthCheck(_options, _memoryCache);
        var checkResult = await healthCheck.CheckHealthAsync(new HealthCheckContext { Registration = new HealthCheckRegistration("Test", (x) => null, HealthStatus.Degraded, new string[] { }) });
        Assert.Equal(HealthStatus.Degraded, checkResult.Status);
    }
}