using DddDotNet.Infrastructure.HealthChecks;
using DddDotNet.Infrastructure.Messaging.AzureQueueStorage;
using DddDotNet.Infrastructure.Messaging.AzureServiceBus;
using DddDotNet.Infrastructure.Messaging.Kafka;
using DddDotNet.Infrastructure.Messaging.RabbitMQ;
using DddDotNet.Infrastructure.Notification.Email.Smtp;
using DddDotNet.Infrastructure.Notification.Web.SignalR;
using DddDotNet.Infrastructure.Storages.Amazon;
using DddDotNet.Infrastructure.Storages.Azure;
using DddDotNet.Infrastructure.Storages.Google;
using DddDotNet.Infrastructure.Storages.Local;
using DddDotNet.Infrastructure.Storages.Sftp;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection;

public static class HealthCheckBuilderExtensions
{
    public static IHealthChecksBuilder AddHttp(
        this IHealthChecksBuilder builder,
        string uri,
        string name = default,
        HealthStatus? failureStatus = default,
        IEnumerable<string> tags = default,
        TimeSpan? timeout = default)
    {
        return builder.Add(new HealthCheckRegistration(
            name,
            new HttpHealthCheck(uri),
            failureStatus,
            tags,
            timeout));
    }

    public static IHealthChecksBuilder AddSqlServer(
        this IHealthChecksBuilder builder,
        string connectionString,
        string healthQuery = default,
        string name = default,
        HealthStatus? failureStatus = default,
        IEnumerable<string> tags = default,
        TimeSpan? timeout = default)
    {
        return builder.Add(new HealthCheckRegistration(
            name,
            new SqlServerHealthCheck(connectionString, healthQuery),
            failureStatus,
            tags,
            timeout));
    }

    public static IHealthChecksBuilder AddAmazonS3(
        this IHealthChecksBuilder builder,
        AmazonOptions amazonOptions,
        string name = default,
        HealthStatus? failureStatus = default,
        IEnumerable<string> tags = default,
        TimeSpan? timeout = default)
    {
        return builder.Add(new HealthCheckRegistration(
            name,
            new AmazonS3HealthCheck(amazonOptions),
            failureStatus,
            tags,
            timeout));
    }

    public static IHealthChecksBuilder AddAzureBlobStorage(
        this IHealthChecksBuilder builder,
        AzureBlobOptions azureBlobOptions,
        string name = default,
        HealthStatus? failureStatus = default,
        IEnumerable<string> tags = default,
        TimeSpan? timeout = default)
    {
        return builder.Add(new HealthCheckRegistration(
            name,
            new AzureBlobStorageHealthCheck(azureBlobOptions),
            failureStatus,
            tags,
            timeout));
    }

    public static IHealthChecksBuilder AddGoogleCloudStorage(
        this IHealthChecksBuilder builder,
        GoogleCloudStorageOptions googleCloudOptions,
        string name = default,
        HealthStatus? failureStatus = default,
        IEnumerable<string> tags = default,
        TimeSpan? timeout = default)
    {
        return builder.Add(new HealthCheckRegistration(
            name,
            new GoogleCloudStorageHealthCheck(googleCloudOptions),
            failureStatus,
            tags,
            timeout));
    }

    public static IHealthChecksBuilder AddAzureQueueStorage(
        this IHealthChecksBuilder builder,
        string queueUrl,
        string name = default,
        HealthStatus? failureStatus = default,
        IEnumerable<string> tags = default,
        TimeSpan? timeout = default)
    {
        return builder.Add(new HealthCheckRegistration(
            name,
            new AzureQueueStorageHealthCheck(queueUrl),
            failureStatus,
            tags,
            timeout));
    }

    public static IHealthChecksBuilder AddAzureServiceBus(
        this IHealthChecksBuilder builder,
        string @namespace, IReadOnlyCollection<int> ports,
        string name = default,
        HealthStatus? failureStatus = default,
        IEnumerable<string> tags = default,
        TimeSpan? timeout = default)
    {
        return builder.Add(new HealthCheckRegistration(
            name,
            new AzureServiceBusHealthCheck(@namespace, ports),
            failureStatus,
            tags,
            timeout));
    }

    public static IHealthChecksBuilder AddKafka(
        this IHealthChecksBuilder builder,
        string bootstrapServers,
        string topic,
        string name = default,
        HealthStatus? failureStatus = default,
        IEnumerable<string> tags = default,
        TimeSpan? timeout = default)
    {
        return builder.Add(new HealthCheckRegistration(
            name,
            new KafkaHealthCheck(bootstrapServers: bootstrapServers, topic: topic),
            failureStatus,
            tags,
            timeout));
    }

    public static IHealthChecksBuilder AddRabbitMQ(
        this IHealthChecksBuilder builder,
        RabbitMQHealthCheckOptions options,
        string name = default,
        HealthStatus? failureStatus = default,
        IEnumerable<string> tags = default,
        TimeSpan? timeout = default)
    {
        return builder.Add(new HealthCheckRegistration(
            name,
            new RabbitMQHealthCheck(options),
            failureStatus,
            tags,
            timeout));
    }

    public static IHealthChecksBuilder AddSmtp(
        this IHealthChecksBuilder builder,
        SmtpHealthCheckOptions options,
        string name = default,
        HealthStatus? failureStatus = default,
        IEnumerable<string> tags = default,
        TimeSpan? timeout = default)
    {
        return builder.Add(new HealthCheckRegistration(
            name,
            new SmtpHealthCheck(options),
            failureStatus,
            tags,
            timeout));
    }

    public static IHealthChecksBuilder AddLocalFile(
        this IHealthChecksBuilder builder,
        LocalFileHealthCheckOptions options,
        string name = default,
        HealthStatus? failureStatus = default,
        IEnumerable<string> tags = default,
        TimeSpan? timeout = default)
    {
        return builder.Add(new HealthCheckRegistration(
            name,
            new LocalFileHealthCheck(options),
            failureStatus,
            tags,
            timeout));
    }

    public static IHealthChecksBuilder AddSftp(
        this IHealthChecksBuilder builder,
        SftpOptions options,
        string name = default,
        HealthStatus? failureStatus = default,
        IEnumerable<string> tags = default,
        TimeSpan? timeout = default)
    {
        return builder.Add(new HealthCheckRegistration(
            name,
            new SftpStorageHealthCheck(options),
            failureStatus,
            tags,
            timeout));
    }

    public static IHealthChecksBuilder AddSignalR(
        this IHealthChecksBuilder builder,
        string endPoint,
        string hubName,
        string eventName,
        string name = default,
        HealthStatus? failureStatus = default,
        IEnumerable<string> tags = default,
        TimeSpan? timeout = default)
    {
        return builder.Add(new HealthCheckRegistration(
            name,
            new SignalRHealthCheck(
                endPoint: endPoint,
                hubName: hubName,
                eventName: eventName),
            failureStatus,
            tags,
            timeout));
    }
}