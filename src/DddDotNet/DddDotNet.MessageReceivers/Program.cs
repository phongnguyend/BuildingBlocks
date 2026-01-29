using Azure.Messaging.EventGrid;
using Azure.Storage.Queues;
using DddDotNet.Infrastructure.Messaging.AmazonSQS;
using DddDotNet.Infrastructure.Messaging.ApacheActiveMQ;
using DddDotNet.Infrastructure.Messaging.AzureQueueStorage;
using DddDotNet.Infrastructure.Messaging.GooglePubSub;
using DddDotNet.Infrastructure.Messaging.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading.Tasks;


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
var amazonSqs = new AmazonSqsReceiver<Program, Message>(amazonSqsOptions);
_ = amazonSqs.ReceiveAsync(async (message, metaData, cancellationToken) =>
{
    Console.WriteLine($"AmazonSqs: {message}");
    await Task.CompletedTask;
});

var apacheActiveMqOptions = new ApacheActiveMQOptions();
config.GetSection("Messaging:ApacheActiveMQ").Bind(apacheActiveMqOptions);
var apacheActiveMq = new ApacheActiveMQReceiver<Program, Message>(apacheActiveMqOptions);
_ = apacheActiveMq.ReceiveAsync(async (message, metaData, cancellationToken) =>
{
    Console.WriteLine($"ApacheActiveMQ: {message}");
    await Task.CompletedTask;
});

var azureQueueEventGridOptions = new AzureQueueStorageOptions
{
    ConnectionString = config["Messaging:AzureQueue:ConnectionString"],
    QueueName = "event-grid-integration-test",
    QueueClientOptions = new QueueClientOptions
    {
        MessageEncoding = QueueMessageEncoding.Base64
    }
};
var azureQueueEventGrid = new AzureQueueStorageReceiver<Program, EventGridEvent>(azureQueueEventGridOptions);
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
var googlePubSub = new GooglePubSubReceiver<Program, Message>(googlePubSubOptions);
_ = googlePubSub.ReceiveAsync(async (message, metaData, cancellationToken) =>
{
    Console.WriteLine($"GooglePubSub: {message}");
    await Task.CompletedTask;
});

var kafka = new KafkaReceiver<Program, Message>(new KafkaReceiverOptions
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