using DddDotNet.Domain.Infrastructure.Messaging;
using DddDotNet.Infrastructure.Messaging;
using DddDotNet.Infrastructure.Messaging.AzureEventGrid;
using DddDotNet.Infrastructure.Messaging.AzureEventHub;
using DddDotNet.Infrastructure.Messaging.AzureQueue;
using DddDotNet.Infrastructure.Messaging.AzureServiceBus;
using DddDotNet.Infrastructure.Messaging.Fake;
using DddDotNet.Infrastructure.Messaging.Kafka;
using DddDotNet.Infrastructure.Messaging.RabbitMQ;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection;

public static class MessagingCollectionExtensions
{
    public static IServiceCollection AddAzureEventGridSender<T>(this IServiceCollection services, AzureEventGridsOptions options)
    {
        var gridOptions = new AzureEventGridOptions
        {
            DomainEndpoint = options.DomainEndpoint,
            DomainKey = options.DomainKey,
            Topic = options.Topics[typeof(T).Name]
        };
        services.AddSingleton<IMessageSender<T>>(new AzureEventGridSender<T>(gridOptions));
        return services;
    }

    public static IServiceCollection AddAzureEventHubSender<T>(this IServiceCollection services, AzureEventHubsOptions options)
    {
        var hubOptions = new AzureEventHubOptions
        {
            ConnectionString = options.ConnectionString,
            HubName = options.Hubs[typeof(T).Name],
            StorageConnectionString = options.StorageConnectionString,
            StorageContainerName = options.StorageContainerNames != null && options.StorageContainerNames.ContainsKey(typeof(T).Name) ? options.StorageContainerNames[typeof(T).Name] : null
        };
        services.AddSingleton<IMessageSender<T>>(new AzureEventHubSender<T>(hubOptions));
        return services;
    }

    public static IServiceCollection AddAzureEventHubReceiver<TConsumer, T>(this IServiceCollection services, AzureEventHubsOptions options)
    {
        var hubOptions = new AzureEventHubOptions
        {
            ConnectionString = options.ConnectionString,
            HubName = options.Hubs[typeof(T).Name],
            StorageConnectionString = options.StorageConnectionString,
            StorageContainerName = options.StorageContainerNames != null && options.StorageContainerNames.ContainsKey(typeof(T).Name) ? options.StorageContainerNames[typeof(T).Name] : null
        };
        services.AddTransient<IMessageReceiver<TConsumer, T>>(x => new AzureEventHubReceiver<TConsumer, T>(hubOptions));
        return services;
    }

    public static IServiceCollection AddAzureQueueSender<T>(this IServiceCollection services, AzureQueuesOptions options)
    {
        var queueOptions = new AzureQueueOptions
        {
            ConnectionString = options.ConnectionString,
            QueueName = options.QueueNames[typeof(T).Name],
            QueueClientOptions = options.QueueClientOptions
        };
        services.AddSingleton<IMessageSender<T>>(new AzureQueueSender<T>(queueOptions));
        return services;
    }

    public static IServiceCollection AddAzureQueueReceiver<TConsumer, T>(this IServiceCollection services, AzureQueuesOptions options)
    {
        var queueOptions = new AzureQueueOptions
        {
            ConnectionString = options.ConnectionString,
            QueueName = options.QueueNames[typeof(T).Name],
            QueueClientOptions = options.QueueClientOptions
        };
        services.AddTransient<IMessageReceiver<TConsumer, T>>(x => new AzureQueueReceiver<TConsumer, T>(queueOptions));
        return services;
    }

    public static IServiceCollection AddAzureServiceBusQueueSender<T>(this IServiceCollection services, AzureServiceBusOptions options)
    {
        var queueOptions = new AzureServiceBusQueueOptions
        {
            ConnectionString = options.ConnectionString,
            Namespace = options.Namespace,
            QueueName = options.QueueNames[typeof(T).Name]
        };
        services.AddSingleton<IMessageSender<T>>(new AzureServiceBusQueueSender<T>(queueOptions));
        return services;
    }

    public static IServiceCollection AddAzureServiceBusQueueReceiver<TConsumer, T>(this IServiceCollection services, AzureServiceBusOptions options)
    {
        var queueOptions = new AzureServiceBusQueueOptions
        {
            ConnectionString = options.ConnectionString,
            Namespace = options.Namespace,
            QueueName = options.QueueNames[typeof(T).Name]
        };
        services.AddTransient<IMessageReceiver<TConsumer, T>>(x => new AzureServiceBusQueueReceiver<TConsumer, T>(queueOptions));
        return services;
    }

    public static IServiceCollection AddFakeSender<T>(this IServiceCollection services)
    {
        services.AddSingleton<IMessageSender<T>>(new FakeSender<T>());
        return services;
    }

    public static IServiceCollection AddFakeReceiver<TConsumer, T>(this IServiceCollection services)
    {
        services.AddTransient<IMessageReceiver<TConsumer, T>>(x => new FakeReceiver<TConsumer, T>());
        return services;
    }

    public static IServiceCollection AddKafkaSender<T>(this IServiceCollection services, KafkaOptions options)
    {
        services.AddSingleton<IMessageSender<T>>(new KafkaSender<T>(options.BootstrapServers, options.Topics[typeof(T).Name]));
        return services;
    }

    public static IServiceCollection AddKafkaReceiver<TConsumer, T>(this IServiceCollection services, KafkaOptions options)
    {
        services.AddTransient<IMessageReceiver<TConsumer, T>>(x => new KafkaReceiver<TConsumer, T>(new KafkaReceiverOptions
        {
            BootstrapServers = options.BootstrapServers,
            Topic = options.Topics[typeof(T).Name],
            GroupId = options.GroupId
        }));
        return services;
    }

    public static IServiceCollection AddRabbitMQSender<T>(this IServiceCollection services, RabbitMQOptions options)
    {
        services.AddSingleton<IMessageSender<T>>(new RabbitMQSender<T>(new RabbitMQSenderOptions
        {
            HostName = options.HostName,
            UserName = options.UserName,
            Password = options.Password,
            ExchangeName = options.ExchangeName,
            RoutingKey = options.RoutingKeys[typeof(T).Name],
            MessageEncryptionEnabled = options.MessageEncryptionEnabled,
            MessageEncryptionKey = options.MessageEncryptionKey
        }));
        return services;
    }

    public static IServiceCollection AddRabbitMQReceiver<TConsumer, T>(this IServiceCollection services, RabbitMQOptions options)
    {
        var receiverOptions = new RabbitMQReceiverOptions
        {
            HostName = options.HostName,
            UserName = options.UserName,
            Password = options.Password,
            ExchangeName = options.ExchangeName,
            RoutingKey = options.RoutingKeys[typeof(T).Name],
            QueueName = options.Consumers[typeof(TConsumer).Name][typeof(T).Name],
            AutomaticCreateEnabled = true,
            MessageEncryptionKey = options.MessageEncryptionKey,
            MaxRetryCount = options.GetMaxRetryCount(typeof(TConsumer).Name),
            RetryIntervals = options.GetRetryIntervals(typeof(TConsumer).Name),
            DeadLetterEnabled = options.GetDeadLetterEnabled(typeof(TConsumer).Name)
        };

        services.AddTransient<IMessageReceiver<TConsumer, T>>(x => new RabbitMQReceiver<TConsumer, T>(receiverOptions, x.GetRequiredService<ILogger<RabbitMQReceiver<TConsumer, T>>>()));
        return services;
    }

    public static IServiceCollection AddMessageBusSender<T>(this IServiceCollection services, MessagingOptions options)
    {
        if (options.UsedRabbitMQ())
        {
            services.AddRabbitMQSender<T>(options.RabbitMQ);
        }
        else if (options.UsedKafka())
        {
            services.AddKafkaSender<T>(options.Kafka);
        }
        else if (options.UsedAzureQueue())
        {
            services.AddAzureQueueSender<T>(options.AzureQueue);
        }
        else if (options.UsedAzureServiceBus())
        {
            services.AddAzureServiceBusQueueSender<T>(options.AzureServiceBus);
        }
        else if (options.UsedAzureEventGrid())
        {
            services.AddAzureEventGridSender<T>(options.AzureEventGrid);
        }
        else if (options.UsedAzureEventHub())
        {
            services.AddAzureEventHubSender<T>(options.AzureEventHub);
        }
        else if (options.UsedFake())
        {
            services.AddFakeSender<T>();
        }

        return services;
    }

    public static IServiceCollection AddMessageBusReceiver<TConsumer, T>(this IServiceCollection services, MessagingOptions options)
    {
        if (options.UsedRabbitMQ())
        {
            services.AddRabbitMQReceiver<TConsumer, T>(options.RabbitMQ);
        }
        else if (options.UsedKafka())
        {
            services.AddKafkaReceiver<TConsumer, T>(options.Kafka);
        }
        else if (options.UsedAzureQueue())
        {
            services.AddAzureQueueReceiver<TConsumer, T>(options.AzureQueue);
        }
        else if (options.UsedAzureServiceBus())
        {
            services.AddAzureServiceBusQueueReceiver<TConsumer, T>(options.AzureServiceBus);
        }
        else if (options.UsedAzureEventHub())
        {
            services.AddAzureEventHubReceiver<TConsumer, T>(options.AzureEventHub);
        }
        else if (options.UsedFake())
        {
            services.AddFakeReceiver<TConsumer, T>();
        }

        return services;
    }

    public static IHealthChecksBuilder AddMessageBusHealthCheck(this IHealthChecksBuilder healthChecksBuilder, MessagingOptions options)
    {
        if (options.UsedRabbitMQ())
        {
            var name = "Message Broker (RabbitMQ)";

            healthChecksBuilder.AddRabbitMQ(new RabbitMQHealthCheckOptions
            {
                HostName = options.RabbitMQ.HostName,
                UserName = options.RabbitMQ.UserName,
                Password = options.RabbitMQ.Password,
            },
            name: name,
            failureStatus: HealthStatus.Degraded);
        }
        else if (options.UsedKafka())
        {
            var name = "Message Broker (Kafka)";
            healthChecksBuilder.AddKafka(
                bootstrapServers: options.Kafka.BootstrapServers,
                topic: "healthcheck",
                name: name,
                failureStatus: HealthStatus.Degraded);
        }
        else if (options.UsedAzureQueue())
        {
            foreach (var queueName in options.AzureQueue.QueueNames)
            {
                var queueOptions = new AzureQueueOptions
                {
                    ConnectionString = options.AzureQueue.ConnectionString,
                    QueueName = queueName.Value,
                    QueueClientOptions = options.AzureQueue.QueueClientOptions
                };
                healthChecksBuilder.AddAzureQueueStorage(
                    queueOptions,
                    name: $"Message Broker (Azure Queue) {queueName.Key}",
                    failureStatus: HealthStatus.Degraded);
            }
        }
        else if (options.UsedAzureServiceBus())
        {
            foreach (var queueName in options.AzureServiceBus.QueueNames)
            {
                var queueOptions = new AzureServiceBusQueueOptions
                {
                    ConnectionString = options.AzureServiceBus.ConnectionString,
                    Namespace = options.AzureServiceBus.Namespace,
                    QueueName = queueName.Value
                };
                healthChecksBuilder.AddAzureServiceBus(
                    options.AzureServiceBus.Namespace,
                    [5672],
                    name: $"Message Broker (Azure Service Bus) {queueName.Key}",
                    failureStatus: HealthStatus.Degraded);
            }
        }
        else if (options.UsedAzureEventGrid())
        {
            // TODO: Add Health Check
        }
        else if (options.UsedAzureEventHub())
        {
            // TODO: Add Health Check
        }
        else if (options.UsedFake())
        {
        }

        return healthChecksBuilder;
    }
}
