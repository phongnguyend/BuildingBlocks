using DddDotNet.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UriHelper;

namespace DddDotNet.Infrastructure.Storages.SharePointOnline;

public class SharePointOnlineHealthCheck : IHealthCheck
{
    private readonly GraphServiceClient _client;
    private readonly SharePointOnlineOptions _options;
    private readonly IMemoryCache _memoryCache;

    public SharePointOnlineHealthCheck(SharePointOnlineOptions options, IMemoryCache memoryCache)
    {
        _client = options.CreateGraphServiceClient();
        _options = options;
        _memoryCache = memoryCache;
    }

    private async Task<Site> GetSiteAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = $"SharePointSite_{_options.SiteHostname}_{_options.SitePath}";

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
            SlidingExpiration = TimeSpan.FromMinutes(5)
        };

        return await _memoryCache.GetOrSetAsync(cacheKey, async () =>
        {
            var site = await _client.Sites[$"{_options.SiteHostname}:{_options.SitePath}"]
                .GetAsync(cancellationToken: cancellationToken);
            return site;
        }, cacheOptions);
    }

    private async Task<Drive> GetDocumentLibraryAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = $"SharePointDrive_{_options.SiteHostname}_{_options.SitePath}_{_options.DocumentLibraryName}";

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
            SlidingExpiration = TimeSpan.FromMinutes(5)
        };

        return await _memoryCache.GetOrSetAsync(cacheKey, async () =>
        {
            var site = await GetSiteAsync(cancellationToken);

            var drives = await _client.Sites[site.Id].Drives
                .GetAsync(cancellationToken: cancellationToken);

            var drive = drives.Value.FirstOrDefault(d =>
                d.Name.Equals(_options.DocumentLibraryName, StringComparison.OrdinalIgnoreCase));

            if (drive == null)
            {
                throw new InvalidOperationException($"Document library '{_options.DocumentLibraryName}' not found");
            }

            return drive;
        }, cacheOptions);
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Test connection by getting the site information using cached methods
            var site = await GetSiteAsync(cancellationToken);

            // Get the document library using cached methods
            var drive = await GetDocumentLibraryAsync(cancellationToken);

            // Test file operations
            var fileName = UriPath.Combine(_options.Path, $"HealthCheck/{DateTime.Now:yyyy-MM-dd-hh-mm-ss}-{Guid.NewGuid()}.txt");
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes($"HealthCheck {DateTime.Now}"));

            // Upload test file
            await _client.Drives[drive.Id].Root
                .ItemWithPath(fileName)
                .Content
                .PutAsync(stream, cancellationToken: cancellationToken);

            // Delete test file
            await _client.Drives[drive.Id].Root
                .ItemWithPath(fileName)
                .DeleteAsync(cancellationToken: cancellationToken);

            return HealthCheckResult.Healthy($"Site: {_options.SiteHostname}:{_options.SitePath}, Library: {_options.DocumentLibraryName}");
        }
        catch (Exception exception)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, null, exception);
        }
    }
}