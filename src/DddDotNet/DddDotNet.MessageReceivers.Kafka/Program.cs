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