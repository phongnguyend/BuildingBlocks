using DddDotNet.Infrastructure.Storages;
using DddDotNet.Infrastructure.Storages.Amazon;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UriHelper;
using Xunit;

namespace DddDotNet.IntegrationTests.Infrastructure.Storages;

public class AmazonS3StorageManagerTests
{
    AmazonOptions _options = new AmazonOptions();

    public AmazonS3StorageManagerTests()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddUserSecrets("09f024f8-e8d1-4b78-9ddd-da941692e8fa")
            .Build();

        config.GetSection("Storage:Amazon").Bind(_options);
    }

    [Fact]
    public async Task CreateAsync_Success()
    {
        AmazonS3StorageManager amazonS3StorageManager = new AmazonS3StorageManager(_options);

        var fileEntry = new FileEntry
        {
            FileLocation = UriPath.Combine(DateTime.Now.ToString("yyyy/MM/dd"), Guid.NewGuid().ToString())
        };

        var fileStream = new MemoryStream(Encoding.UTF8.GetBytes("Test"));

        await amazonS3StorageManager.CreateAsync(fileEntry, fileStream);

        var content1 = Encoding.UTF8.GetString(await amazonS3StorageManager.ReadAsync(fileEntry));

        fileStream = new MemoryStream(Encoding.UTF8.GetBytes("Test2"));

        await amazonS3StorageManager.CreateAsync(fileEntry, fileStream);

        var content2 = Encoding.UTF8.GetString(await amazonS3StorageManager.ReadAsync(fileEntry));

        await amazonS3StorageManager.ArchiveAsync(fileEntry);

        await amazonS3StorageManager.UnArchiveAsync(fileEntry);

        var path = Path.GetTempFileName();
        await amazonS3StorageManager.DownloadAsync(fileEntry, path);
        var content3 = File.ReadAllText(path);
        File.Delete(path);

        path = Path.GetTempFileName();
        using (var tempFileStream = File.OpenWrite(path))
        {
            await amazonS3StorageManager.DownloadAsync(fileEntry, tempFileStream);
        }
        var content4 = File.ReadAllText(path);
        File.Delete(path);

        await amazonS3StorageManager.DeleteAsync(fileEntry);

        Assert.Equal("Test", content1);
        Assert.Equal("Test2", content2);
        Assert.Equal("Test2", content3);
        Assert.Equal("Test2", content4);
    }

    [Fact]
    public async Task HealthCheck_Healthy()
    {
        var healthCheck = new AmazonS3HealthCheck(_options);
        var checkResult = await healthCheck.CheckHealthAsync(null);
        Assert.Equal(HealthStatus.Healthy, checkResult.Status);
    }

    [Fact]
    public async Task HealthCheck_Degraded()
    {
        _options.BucketName = "fsfsafafsfsfsfsfs";
        var healthCheck = new AmazonS3HealthCheck(_options);
        var checkResult = await healthCheck.CheckHealthAsync(new HealthCheckContext { Registration = new HealthCheckRegistration("Test", (x) => null, HealthStatus.Degraded, new string[] { }) });
        Assert.Equal(HealthStatus.Degraded, checkResult.Status);
    }
}
