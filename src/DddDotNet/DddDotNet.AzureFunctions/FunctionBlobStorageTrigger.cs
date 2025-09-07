using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DddDotNet.AzureFunctions;

public class FunctionBlobStorageTrigger
{
    private readonly ILogger<FunctionBlobStorageTrigger> _logger;

    public FunctionBlobStorageTrigger(ILogger<FunctionBlobStorageTrigger> logger)
    {
        _logger = logger;
    }

    [Function(nameof(FunctionBlobStorageTrigger))]
    public async Task Run(
        [BlobTrigger("uploads/{name}", Connection = "AzureBlobStorageConnectionString")] Stream blob,
        string name)
    {
        _logger.LogInformation($"Blob trigger function processed blob\n Name: {name} \n Size: {blob.Length} Bytes");
        
        // Example: Read blob content
        using var reader = new StreamReader(blob);
        var content = await reader.ReadToEndAsync();
        
        _logger.LogInformation($"Blob content preview (first 100 chars): {content.Substring(0, Math.Min(content.Length, 100))}");
        
        // Add your blob processing logic here
        // For example:
        // - Parse the blob content
        // - Transform data
        // - Send to other services
        // - Store processed results
    }
}
