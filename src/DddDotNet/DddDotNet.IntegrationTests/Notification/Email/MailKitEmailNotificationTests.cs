using DddDotNet.Infrastructure.Notification.Email;
using DddDotNet.Infrastructure.Notification.Email.MailKit;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DddDotNet.IntegrationTests.Notification.Email;

public class MailKitEmailNotificationTests
{
    public MailKitEmailNotificationTests()
    {
    }

    [Fact]
    public async Task SendAsync_Success()
    {
        MailKitEmailNotification mailKitClient = new MailKitEmailNotification(new MailKitOptions
        {
            Host = "localhost",
        });

        await mailKitClient.SendAsync(new EmailMessage
        {
            From = "phong@gmail.com",
            Tos = "phong@gmail.com",
            Subject = "IntegrationTests 1",
            Body = "IntegrationTests 1",
            Attachments = [new Attachment { FileName = "test.txt", Content = new MemoryStream(Encoding.UTF8.GetBytes("This is a text file.")) }]
        });

        await mailKitClient.SendAsync(new EmailMessage
        {
            From = "phong@gmail.com",
            FromName = "",
            Tos = "phong@gmail.com",
            Subject = "IntegrationTests 2",
            Body = "IntegrationTests 2",
        });

        await mailKitClient.SendAsync(new EmailMessage
        {
            From = "phong@gmail.com",
            FromName = "Phong Nguyen",
            Tos = "phong@gmail.com",
            Subject = "IntegrationTests 3",
            Body = "IntegrationTests 3",
        });
    }

    [Fact]
    public async Task HealthCheck_Healthy()
    {
        var healthCheck = new MailKitHealthCheck(new MailKitHealthCheckOptions
        {
            Host = "localhost",
            From = "phong@gmail.com",
            FromName = "Phong Nguyen",
            Tos = "phong@gmail.com",
            Subject = "HealthCheck",
            Body = "HealthCheck",
        });
        var checkResult = await healthCheck.CheckHealthAsync(new HealthCheckContext { Registration = new HealthCheckRegistration("Test", (x) => null, HealthStatus.Degraded, new string[] { }) });
        Assert.Equal(HealthStatus.Healthy, checkResult.Status);
    }

    [Fact]
    public async Task HealthCheck_Degraded()
    {
        var healthCheck = new MailKitHealthCheck(new MailKitHealthCheckOptions
        {
            Host = Guid.NewGuid().ToString(),
            From = "phong@gmail.com",
            FromName = "Phong Nguyen",
            Tos = "phong@gmail.com",
            Subject = "HealthCheck",
            Body = "HealthCheck",
        });
        var checkResult = await healthCheck.CheckHealthAsync(new HealthCheckContext { Registration = new HealthCheckRegistration("Test", (x) => null, HealthStatus.Degraded, new string[] { }) });
        Assert.Equal(HealthStatus.Degraded, checkResult.Status);
    }
}