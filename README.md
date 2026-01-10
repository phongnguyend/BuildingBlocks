## Identity Providers
  | Identity  | Status | Health Check | Path |
  | --------- | :----: | :----------: | ---- |
  | Auth0 by Okta| ✅ | | [/Identity/Auth0](/src/DddDotNet/DddDotNet.Infrastructure/Identity/Auth0) |
  | AWS Cognito| ✅ | | [/Identity/Amazon](/src/DddDotNet/DddDotNet.Infrastructure/Identity/Amazon) |
  | Azure Active Directory| ✅ | | [/Identity/Azure](/src/DddDotNet/DddDotNet.Infrastructure/Identity/Azure) |
  | Azure Active Directory B2C| ✅ | | [/Identity/Azure](/src/DddDotNet/DddDotNet.Infrastructure/Identity/Azure) |
  | Google Cloud Identity Platform| ✅ | | [/Identity/GoogleCloud](/src/DddDotNet/DddDotNet.Infrastructure/Identity/GoogleCloud) |

## Storage Providers
  | Storage  | Status | Health Check | Path |
  | -------- | :----: | :----------: | ---- |
  | Amazon S3 | ✅ | ✅ | [/Storages/Amazon](/src/DddDotNet/DddDotNet.Infrastructure/Storages/Amazon) |
  | Azure Blob Storage| ✅ | ✅ | [/Storages/Azure](/src/DddDotNet/DddDotNet.Infrastructure/Storages/Azure) |
  | Azure File Share | ✅ | | [/Storages/Azure](/src/DddDotNet/DddDotNet.Infrastructure/Storages/Azure) |
  | FTP / FTPS | ✅ | ✅ | [/Storages/Ftp](/src/DddDotNet/DddDotNet.Infrastructure/Storages/Ftp) |
  | Google Cloud Storage | ✅ | ✅ | [/Storages/Google](/src/DddDotNet/DddDotNet.Infrastructure/Storages/Google) |
  | Local | ✅ | ✅ | [/Storages/Local](/src/DddDotNet/DddDotNet.Infrastructure/Storages/Local) |
  | SFTP | ✅ | ✅ | [/Storages/Sftp](/src/DddDotNet/DddDotNet.Infrastructure/Storages/Sftp) |
  | SharePointOnline | ✅ | ✅ | [/Storages/SharePointOnline](/src/DddDotNet/DddDotNet.Infrastructure/Storages/SharePointOnline) |
  | SMB | ✅ | | [/Storages/Smb](/src/DddDotNet/DddDotNet.Infrastructure/Storages/Smb) |
  | Win32 Network Share | ✅ | | [/Storages/WindowsNetworkShare](/src/DddDotNet/DddDotNet.Infrastructure/Storages/WindowsNetworkShare) |
  
## Messaging Providers
  | Message Broker  | Status | Encryption | Health Check | Path |
  | --------------- | :----: | :--------: | :----------: | ---- |
  | Amazon Event Bridge | ✅ | | ✅ | [/Messaging/AmazonEventBridge](/src/DddDotNet/DddDotNet.Infrastructure/Messaging/AmazonEventBridge) |
  | Amazon Kinesis | ✅ | | ✅ | [/Messaging/AmazonKinesis](/src/DddDotNet/DddDotNet.Infrastructure/Messaging/AmazonKinesis) |
  | Amazon SNS | ✅ | | ✅ | [/Messaging/AmazonSNS](/src/DddDotNet/DddDotNet.Infrastructure/Messaging/AmazonSNS) |
  | Amazon SQS | ✅ | | ✅ | [/Messaging/AmazonSQS](/src/DddDotNet/DddDotNet.Infrastructure/Messaging/AmazonSQS) |
  | Apache ActiveMQ | ✅ | | ✅ | [/Messaging/ApacheActiveMQ](/src/DddDotNet/DddDotNet.Infrastructure/Messaging/ApacheActiveMQ) |
  | Azure Event Grid | ✅ | | ✅ | [/Messaging/AzureEventGrid](/src/DddDotNet/DddDotNet.Infrastructure/Messaging/AzureEventGrid) |
  | Azure Event Hub | ✅ | | ✅ | [/Messaging/AzureEventHub](/src/DddDotNet/DddDotNet.Infrastructure/Messaging/AzureEventHub) |
  | Azure Queue Storage| ✅ | | ✅ | [/Messaging/AzureQueue](/src/DddDotNet/DddDotNet.Infrastructure/Messaging/AzureQueue) |
  | Azure Service Bus | ✅ | | ✅ | [/Messaging/AzureServiceBus](/src/DddDotNet/DddDotNet.Infrastructure/Messaging/AzureServiceBus) |
  | Google Cloud Pub/Sub | ✅ | | ✅ | [/Messaging/GooglePubSub](/src/DddDotNet/DddDotNet.Infrastructure/Messaging/GooglePubSub) |
  | Kafka | ✅ | | ✅ | [/Messaging/Kafka](/src/DddDotNet/DddDotNet.Infrastructure/Messaging/Kafka) |
  | RabbitMQ | ✅ | ✅ | ✅ | [/Messaging/RabbitMQ](/src/DddDotNet/DddDotNet.Infrastructure/Messaging/RabbitMQ) |

## Email Providers
  | Email  | Status | Health Check | Path |
  | ------ | :----: | :----------: | ---- |
  | Amazon SES | ✅ | ✅ | [/Notification/Email/Amazon](/src/DddDotNet/DddDotNet.Infrastructure/Notification/Email/Amazon) |
  | SendGrid | ✅ | ✅ | [/Notification/Email/SendGrid](/src/DddDotNet/DddDotNet.Infrastructure/Notification/Email/SendGrid) |
  | SMTP | ✅ | ✅ | [/Notification/Email/Smtp](/src/DddDotNet/DddDotNet.Infrastructure/Notification/Email/Smtp) |
  | MailKit | ✅ | ✅ | [/Notification/Email/MailKit](/src/DddDotNet/DddDotNet.Infrastructure/Notification/Email/MailKit) |

## SMS Providers
  | SMS  | Status | Health Check | Path |
  | ---- | :----: | :----------: | ---- |
  | Amazon SNS | ✅ | | [/Notification/Sms/Amazon](/src/DddDotNet/DddDotNet.Infrastructure/Notification/Sms/Amazon) |
  | Azure Communication | ✅ | | [/Notification/Sms/Azure](/src/DddDotNet/DddDotNet.Infrastructure/Notification/Sms/Azure) |
  | Twilio | ✅ | ✅ | [/Notification/Sms/Twilio](/src/DddDotNet/DddDotNet.Infrastructure/Notification/Sms/Twilio) |

## Configuration & Secrets Providers
  | Configuration & Secrets  | Status | Health Check | Path |
  | ------------------------ | :----: | :----------: | ---- |
  | SQL Server | ✅ | | [/Configuration](/src/DddDotNet/DddDotNet.Infrastructure/Configuration) |
  | HashiCorp Vault | ✅ | | [/Configuration](/src/DddDotNet/DddDotNet.Infrastructure/Configuration) |
  | Azure App Configuration | ✅ | | [/Configuration](/src/DddDotNet/DddDotNet.Infrastructure/Configuration) |
  | Azure Key Vault | ✅ | | [/Configuration](/src/DddDotNet/DddDotNet.Infrastructure/Configuration) |
  | AWS Secrets Manager | ✅ | | [/Configuration](/src/DddDotNet/DddDotNet.Infrastructure/Configuration) |
  | AWS Systems Manager | ✅ | | [/Configuration](/src/DddDotNet/DddDotNet.Infrastructure/Configuration) |
  | Google Cloud Secret Manager | ✅ | | [/Configuration](/src/DddDotNet/DddDotNet.Infrastructure/Configuration) |

## Caching Providers
  | Caching  | Status | Health Check | Path |
  | ------------------------ | :----: | :----------: | ---- |
  | InMemory | ✅ | | [/Caching](/src/DddDotNet/DddDotNet.Infrastructure/Caching) |
  | Distributed InMemory | ✅ | | [/Caching](/src/DddDotNet/DddDotNet.Infrastructure/Caching) |
  | Distributed Redis| ✅ | | [/Caching](/src/DddDotNet/DddDotNet.Infrastructure/Caching) |
  | Distributed SqlServer| ✅ | | [/Caching](/src/DddDotNet/DddDotNet.Infrastructure/Caching) |
  | Distributed Cosmos | ✅ | | [/Caching](/src/DddDotNet/DddDotNet.Infrastructure/Caching) |
