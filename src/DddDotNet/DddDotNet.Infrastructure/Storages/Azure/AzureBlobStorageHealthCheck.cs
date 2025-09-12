using Azure.Storage.Blobs;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UriHelper;

namespace DddDotNet.Infrastructure.Storages.Azure;

public class AzureBlobStorageHealthCheck : IHealthCheck
{
    private readonly AzureBlobOptions _option;
    private readonly BlobContainerClient _container;

    public AzureBlobStorageHealthCheck(AzureBlobOptions option)
    {
        _option = option;
        _container = _option.CreateBlobContainerClient();
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var fileName = UriPath.Combine(_option.Path, $"HealthCheck/{DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss")}-{Guid.NewGuid()}.txt");
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes($"HealthCheck {DateTime.Now}"));
            await _container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            BlobClient blob = _container.GetBlobClient(fileName);
            await blob.UploadAsync(stream, overwrite: true, cancellationToken);
            await blob.DeleteAsync(cancellationToken: cancellationToken);

            return HealthCheckResult.Healthy($"ContainerName: {_option.Container}");
        }
        catch (Exception exception)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, null, exception);
        }
    }
}
