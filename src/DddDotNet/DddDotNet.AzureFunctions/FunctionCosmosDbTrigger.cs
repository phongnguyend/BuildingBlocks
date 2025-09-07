using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DddDotNet.AzureFunctions;

public class FunctionCosmosDbTrigger
{
    private readonly ILogger<FunctionCosmosDbTrigger> _logger;

    public FunctionCosmosDbTrigger(ILogger<FunctionCosmosDbTrigger> logger)
    {
        _logger = logger;
    }

    [Function(nameof(FunctionCosmosDbTrigger))]
    public async Task Run(
        [CosmosDBTrigger(
            databaseName: "SampleDatabase",
            containerName: "SampleContainer",
            Connection = "AzureCosmosDbConnectionString",
            LeaseContainerName = "leases",
            CreateLeaseContainerIfNotExists = true)] IReadOnlyList<MyDocument> input)
    {
        _logger.LogInformation($"Cosmos DB trigger function processed {input.Count} documents");

        foreach (var document in input)
        {
            _logger.LogInformation($"Document ID: {document.Id}");
            _logger.LogInformation($"Document content: {JsonSerializer.Serialize(document)}");
            
            // Add your document processing logic here
            // For example:
            // - Validate document structure
            // - Transform data
            // - Send notifications
            // - Update other systems
            // - Calculate aggregations
            
            await ProcessDocumentAsync(document);
        }
    }

    private async Task ProcessDocumentAsync(MyDocument document)
    {
        // Implement your business logic here
        _logger.LogInformation($"Processing document with ID: {document.Id}");
        
        // Example processing logic:
        // - Check document type and route accordingly
        // - Perform validation
        // - Send to downstream services
        // - Update caches
        
        await Task.CompletedTask;
    }
}

// Example document model - customize this based on your Cosmos DB document structure
public class MyDocument
{
    public string Id { get; set; } = string.Empty;
    public string PartitionKey { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public object? Data { get; set; }
    
    // Add additional properties based on your document schema
}
