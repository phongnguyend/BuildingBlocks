﻿namespace DddDotNet.Infrastructure.MessageBrokers.AmazonEventBridge
{
    public class AmazonEventBridgeOptions
    {
        public string AccessKeyID { get; set; }

        public string SecretAccessKey { get; set; }

        public string EndpointId { get; set; }

        public string EndpointName { get; set; }

        public string RegionEndpoint { get; set; }
    }
}
