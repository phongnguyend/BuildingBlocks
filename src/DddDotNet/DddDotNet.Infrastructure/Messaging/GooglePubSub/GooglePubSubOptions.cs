namespace DddDotNet.Infrastructure.Messaging.GooglePubSub;

public class GooglePubSubOptions
{
    public string CredentialFilePath { get; set; }

    public string ProjectId { get; set; }

    public string TopicId { get; set; }

    public string SubscriptionId { get; set; }
}
