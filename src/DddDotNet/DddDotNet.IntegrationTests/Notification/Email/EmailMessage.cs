﻿using DddDotNet.Infrastructure.Notification.Email;

namespace DddDotNet.IntegrationTests.Notification.Email
{
    public class EmailMessage : IEmailMessage
    {
        public string From { get; set; }

        public string FromName { get; set; }

        public string Tos { get; set; }

        public string CCs { get; set; }

        public string BCCs { get; set; }

        public string Subject { get; set; }

        public string Body { get; set; }
    }
}
