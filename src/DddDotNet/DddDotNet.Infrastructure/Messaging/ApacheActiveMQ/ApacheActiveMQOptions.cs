namespace DddDotNet.Infrastructure.Messaging.ApacheActiveMQ;

public class ApacheActiveMQOptions
{
    public string Url { get; set; }

    public string UserName { get; set; }

    public string Password { get; set; }

    public string QueueName { get; set; }

    public string TopicName { get; set; }

    public string SubscriberName { get; set; }

    public bool SharedDurableSubscriber { get; set; }
}
