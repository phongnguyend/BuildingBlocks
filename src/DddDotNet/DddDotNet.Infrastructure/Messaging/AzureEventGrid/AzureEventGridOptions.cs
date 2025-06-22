using System.Collections.Generic;

namespace DddDotNet.Infrastructure.Messaging.AzureEventGrid;

public class AzureEventGridOptions
{
    public string DomainEndpoint { get; set; }

    public string DomainKey { get; set; }

    public Dictionary<string, string> Topics { get; set; }
}
