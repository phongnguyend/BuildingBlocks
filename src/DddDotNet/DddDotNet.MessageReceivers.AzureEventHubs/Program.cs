using DddDotNet.Infrastructure.Messaging.AzureEventHub;
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

var azureEventHubOptions = new AzureEventHubOptions
{
    ConnectionString = config["Messaging:AzureEventHub:ConnectionString"],
    HubName = "integration-test",
    StorageConnectionString = config["Messaging:AzureEventHub:StorageConnectionString"],
    StorageContainerName = "eventhub-integration-test"
};
var azureEventHub = new AzureEventHubReceiver<Program, Message>(azureEventHubOptions);
_ = azureEventHub.ReceiveAsync(async (message, metaData, cancellationToken) =>
{
    Console.WriteLine($"AzureEventHub: {message}");
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