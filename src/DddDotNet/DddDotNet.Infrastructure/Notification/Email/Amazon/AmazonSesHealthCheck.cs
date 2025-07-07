using Amazon.SimpleEmail.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Notification.Email.Amazon;

public class AmazonSesHealthCheck : IHealthCheck
{
    private readonly AmazonSesHealthCheckOptions _options;

    public AmazonSesHealthCheck(AmazonSesHealthCheckOptions options)
    {
        _options = options;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _options.CreateAmazonSimpleEmailServiceClient();

            var sendRequest = new SendEmailRequest
            {
                Source = _options.FromEmail,
                Destination = new Destination
                {
                    ToAddresses = _options.Tos?.Split(';').Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList(),
                },
                Message = new Message
                {
                    Subject = new Content(_options.Subject),
                    Body = new Body
                    {
                        Html = new Content
                        {
                            Charset = "UTF-8",
                            Data = _options.Body,
                        },
                        Text = new Content
                        {
                            Charset = "UTF-8",
                            Data = _options.Body,
                        },
                    },
                },
            };

            var sendEmailResponse = await client.SendEmailAsync(sendRequest);

            if (sendEmailResponse?.HttpStatusCode == HttpStatusCode.OK && !string.IsNullOrWhiteSpace(sendEmailResponse?.MessageId))
            {
                return HealthCheckResult.Healthy();
            }
            else
            {
                return new HealthCheckResult(context.Registration.FailureStatus, $"HttpStatusCode: {sendEmailResponse?.HttpStatusCode}, MessageId: {sendEmailResponse?.MessageId}");
            }

        }
        catch (Exception exception)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, null, exception);
        }
    }
}

public class AmazonSesHealthCheckOptions : AmazonSesOptions
{
    public string Tos { get; set; }

    public string CCs { get; set; }

    public string BCCs { get; set; }

    public string Subject { get; set; }

    public string Body { get; set; }
}
