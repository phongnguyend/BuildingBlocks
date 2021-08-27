﻿using System;

namespace DddDotNet.IntegrationTests.Infrastructure.MessageBrokers
{
    public class Message
    {
        public static Message GetTestMessage()
        {
            return new Message
            {
                Id = Guid.NewGuid(),
                Text1 = "This is Text1",
                Text2 = "This is Text2",
                DateTime1 = DateTime.MinValue,
                DateTime2 = new DateTime(2021, 08, 24),
            };
        }

        public Guid Id { get; set; }

        public string Text1 { get; set; }

        public string Text2 { get; set; }

        public DateTime DateTime1 { get; set; }

        public DateTime DateTime2 { get; set; }
    }
}
