using Azure.Messaging.EventGrid;
using Azure.Storage.Queues;
using DddDotNet.Domain.Infrastructure.Messaging;
using DddDotNet.Infrastructure.Messaging.AmazonSQS;
using DddDotNet.Infrastructure.Messaging.ApacheActiveMQ;
using DddDotNet.Infrastructure.Messaging.AzureEventHub;
using DddDotNet.Infrastructure.Messaging.AzureQueue;
using DddDotNet.Infrastructure.Messaging.AzureServiceBus;
using DddDotNet.Infrastructure.Messaging.GooglePubSub;
using DddDotNet.Infrastructure.Messaging.Kafka;
using DddDotNet.Infrastructure.Messaging.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading.Tasks;

namespace DddDotNet.MessageReceivers;

class Message
{
    public string Id { get; set; }

    public string Text1 { get; set; }

    public string Text2 { get; set; }

    public DateTime DateTime1 { get; set; }

    public DateTime DateTime2 { get; set; }

    public DateTime CreatedDateTime { get; set; }

    public override string ToString()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine();
        stringBuilder.AppendLine($"Id: {Id}");
        stringBuilder.AppendLine($"Text1: {Text1}");
        stringBuilder.AppendLine($"Text2: {Text2}");
        stringBuilder.AppendLine($"DateTime1: {DateTime1}");
        stringBuilder.AppendLine($"DateTime2: {DateTime2}");
        stringBuilder.AppendLine($"CreatedDateTime: {CreatedDateTime}");
        return stringBuilder.ToString();
    }
}

class Program
{
    static void Main(string[] args)
    {
        // Set up dependency injection and logging
        var services = new ServiceCollection();
        services.AddLogging(configure => configure.AddConsole());
        var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetService<ILoggerFactory>();

        var config = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddUserSecrets("09f024f8-e8d1-4b78-9ddd-da941692e8fa")
        .Build();

        var amazonSqsOptions = new AmazonSqsOptions();
        config.GetSection("Messaging:AmazonSQS").Bind(amazonSqsOptions);
        var amazonSqs = new AmazonSqsReceiver<Message>(amazonSqsOptions);
        _ = amazonSqs.ReceiveAsync(async (message, metaData, cancellationToken) =>
        {
            Console.WriteLine($"AmazonSqs: {message}");
            await Task.CompletedTask;
        });

        var apacheActiveMqOptions = new ApacheActiveMQOptions();
        config.GetSection("Messaging:ApacheActiveMQ").Bind(apacheActiveMqOptions);
        var apacheActiveMq = new ApacheActiveMQReceiver<Message>(apacheActiveMqOptions);
        _ = apacheActiveMq.ReceiveAsync(async (message, metaData, cancellationToken) =>
        {
            Console.WriteLine($"ApacheActiveMQ: {message}");
            await Task.CompletedTask;
        });

        var azureQueueOptions = new AzureQueueOptions
        {
            ConnectionString = config["Messaging:AzureQueue:ConnectionString"],
            QueueName = "integration-test"
        };
        var azureQueue = new AzureQueueReceiver<Message>(azureQueueOptions);
        _ = azureQueue.ReceiveAsync(async (message, metaData, cancellationToken) =>
        {
            Console.WriteLine($"AzureQueue: {message}");
            await Task.CompletedTask;
        });

        var azureServiceBusQueueOptions = new AzureServiceBusQueueOptions
        {
            ConnectionString = config["Messaging:AzureServiceBus:ConnectionString"],
            QueueName = "integration-test"
        };
        var azureServiceBusQueue = new AzureServiceBusQueueReceiver<Message>(azureServiceBusQueueOptions);
        _ = azureServiceBusQueue.ReceiveAsync(async (message, metaData, cancellationToken) =>
        {
            Console.WriteLine($"AzureServiceBus: {message}");
            await Task.CompletedTask;
        });

        var azureServiceBusSubscriptionOptions = new AzureServiceBusSubscriptionOptions
        {
            ConnectionString = config["Messaging:AzureServiceBus:ConnectionString"],
            Topic = "topic-integration-test",
            Subscription = "sub-integration-test"
        };
        var azureServiceBusSubscription = new AzureServiceBusSubscriptionReceiver<Message>(azureServiceBusSubscriptionOptions);
        _ = azureServiceBusSubscription.ReceiveAsync(async (message, metaData, cancellationToken) =>
        {
            Console.WriteLine($"AzureServiceBusSubscription: {message}");
            await Task.CompletedTask;
        });

        var azureEventHubOptions = new AzureEventHubOptions
        {
            ConnectionString = config["Messaging:AzureEventHub:ConnectionString"],
            HubName = "integration-test",
            StorageConnectionString = config["Messaging:AzureEventHub:StorageConnectionString"],
            StorageContainerName = "eventhub-integration-test"
        };
        var azureEventHub = new AzureEventHubReceiver<Message>(azureEventHubOptions);
        _ = azureEventHub.ReceiveAsync(async (message, metaData, cancellationToken) =>
        {
            Console.WriteLine($"AzureEventHub: {message}");
            await Task.CompletedTask;
        });

        var azureQueueEventGridOptions = new AzureQueueOptions
        {
            ConnectionString = config["Messaging:AzureQueue:ConnectionString"],
            QueueName = "event-grid-integration-test",
            QueueClientOptions = new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64
            }
        };
        var azureQueueEventGrid = new AzureQueueReceiver<EventGridEvent>(azureQueueEventGridOptions);
        _ = azureQueueEventGrid.ReceiveStringAsync(async (message) =>
        {
            try
            {
                EventGridEvent eventGridEvent = EventGridEvent.Parse(new BinaryData(message));
                Console.WriteLine($"AzureQueueEventGridSubscription: {message}");
                Console.WriteLine($"AzureQueueEventGridSubscription: {eventGridEvent.Data}");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        });

        var googlePubSubOptions = new GooglePubSubOptions();
        config.GetSection("Messaging:GooglePubSub").Bind(googlePubSubOptions);
        var googlePubSub = new GooglePubSubReceiver<Message>(googlePubSubOptions);
        _ = googlePubSub.ReceiveAsync(async (message, metaData, cancellationToken) =>
        {
            Console.WriteLine($"GooglePubSub: {message}");
            await Task.CompletedTask;
        });

        var rabbitMQReceiverOptions = new RabbitMQReceiverOptions()
        {
            AutomaticCreateEnabled = true,
            QueueType = "quorum",
            //MessageEncryptionEnabled = true,
            MessageEncryptionKey = "KEhv7V8VedlhVlNr5vQstLk99l5uflYGB5lamGZd4R4=",
            DeadLetterEnabled = true,
            MaxRetryCount = 3,
            RetryIntervals = [10, 30, 50, 80, 130, 210, 340]
        };
        config.GetSection("Messaging:RabbitMQ").Bind(rabbitMQReceiverOptions);
        var logger = loggerFactory.CreateLogger<RabbitMQReceiver<Message>>();
        var rabbitMqReceiver = new RabbitMQReceiver<Message>(rabbitMQReceiverOptions, logger);
        _ = rabbitMqReceiver.ReceiveAsync(async (message, metaData, cancellationToken) =>
        {
            Console.WriteLine($"RabbitMQ: {message}");
            //throw new Exception("Test exception");
            //throw new ConsumerHandledException { NextAction = ConsumerHandledExceptionNextAction.Retry };
            await Task.CompletedTask;
        });

        var kafka = new KafkaReceiver<Message>(new KafkaReceiverOptions
        {
            BootstrapServers = "localhost:9092",
            Topic = "ddddotnet",
            GroupId = "integrationtest2",
            AutoCommitEnabled = false,
        });
        _ = kafka.ReceiveAsync(async (message, metaData, cancellationToken) =>
        {
            Console.WriteLine($"Kafka: {message}");
            await Task.CompletedTask;
        });

        Console.ReadLine();
    }
}
