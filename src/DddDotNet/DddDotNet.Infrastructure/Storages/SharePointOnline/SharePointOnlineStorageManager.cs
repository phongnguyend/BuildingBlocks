using DddDotNet.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UriHelper;

namespace DddDotNet.Infrastructure.Storages.SharePointOnline;

public class SharePointOnlineStorageManager : IFileStorageManager
{
    private readonly GraphServiceClient _client;
    private readonly SharePointOnlineOptions _options;
    private readonly IMemoryCache _memoryCache;

    public SharePointOnlineStorageManager(SharePointOnlineOptions options, IMemoryCache memoryCache)
    {
        _client = options.CreateGraphServiceClient();
        _options = options;
        _memoryCache = memoryCache;
    }

    private string GetRelativePath(IFileEntry fileEntry)
    {
        return UriPath.Combine(_options.Path, fileEntry.FileLocation);
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

    public async Task CreateAsync(IFileEntry fileEntry, Stream stream, CancellationToken cancellationToken = default)
    {
        var drive = await GetDocumentLibraryAsync(cancellationToken);
        var relativePath = GetRelativePath(fileEntry);

        // Upload the file directly - SharePoint will create directories as needed
        await _client.Drives[drive.Id].Root
            .ItemWithPath(relativePath)
            .Content
            .PutAsync(stream, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
    {
        var drive = await GetDocumentLibraryAsync(cancellationToken);
        var relativePath = GetRelativePath(fileEntry);

        await _client.Drives[drive.Id].Root
            .ItemWithPath(relativePath)
            .DeleteAsync(cancellationToken: cancellationToken);
    }

    public async Task<byte[]> ReadAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream();
        await DownloadAsync(fileEntry, stream, cancellationToken);
        return stream.ToArray();
    }

    public async Task DownloadAsync(IFileEntry fileEntry, string path, CancellationToken cancellationToken = default)
    {
        using var fileStream = File.Create(path);
        await DownloadAsync(fileEntry, fileStream, cancellationToken);
    }

    public async Task DownloadAsync(IFileEntry fileEntry, Stream stream, CancellationToken cancellationToken = default)
    {
        var drive = await GetDocumentLibraryAsync(cancellationToken);
        var relativePath = GetRelativePath(fileEntry);

        var contentStream = await _client.Drives[drive.Id].Root
            .ItemWithPath(relativePath)
            .Content
            .GetAsync(cancellationToken: cancellationToken);

        await contentStream.CopyToAsync(stream, cancellationToken);
    }

    public async Task ArchiveAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
    {
        // SharePoint Online doesn't have a direct archive concept like S3
        // This could be implemented by moving files to an "Archive" folder
        // For now, we'll just implement it as a no-op
        await Task.CompletedTask;
    }

    public async Task UnArchiveAsync(IFileEntry fileEntry, CancellationToken cancellationToken = default)
    {
        // SharePoint Online doesn't have a direct archive concept like S3
        // This would move files back from an "Archive" folder
        // For now, we'll just implement it as a no-op
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}