using Microsoft.Extensions.Diagnostics.HealthChecks;
using Renci.SshNet;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UriHelper;

namespace DddDotNet.Infrastructure.Storages.Sftp;

public class SftpStorageHealthCheck : IHealthCheck
{
    private readonly SftpOptions _options;

    public SftpStorageHealthCheck(SftpOptions options)
    {
        _options = options;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var fileName = UriPath.Combine(_options.Path, $"HealthCheck_{DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss")}_{Guid.NewGuid()}.txt");

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes($"HealthCheck{DateTime.Now}"));
            using var sftpClient = GetSftpClient();
            sftpClient.Connect();
            sftpClient.UploadFile(stream, fileName, canOverride: true);
            sftpClient.DeleteFile(fileName);

            return Task.FromResult(HealthCheckResult.Healthy());
        }
        catch (Exception exception)
        {
            return Task.FromResult(new HealthCheckResult(context.Registration.FailureStatus, exception: exception));
        }
    }

    private SftpClient GetSftpClient()
    {
        var connectionInfo = new ConnectionInfo(_options.Host,
            _options.UserName,
            new PasswordAuthenticationMethod(_options.UserName, _options.Password));
        var client = new SftpClient(connectionInfo);
        return client;
    }
}
