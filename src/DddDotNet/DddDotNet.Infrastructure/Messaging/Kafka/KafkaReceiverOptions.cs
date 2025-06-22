namespace DddDotNet.Infrastructure.Messaging.Kafka;

public class KafkaReceiverOptions
{
    public string BootstrapServers { get; set; }

    public string Topic { get; set; }

    public string GroupId { get; set; }

    public bool? AutoCommitEnabled { get; set; }
}
