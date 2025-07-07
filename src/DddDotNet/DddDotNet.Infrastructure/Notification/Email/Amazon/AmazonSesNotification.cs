using Amazon.SimpleEmail.Model;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Notification.Email.Amazon;

public class AmazonSesNotification : IEmailNotification
{
    private readonly AmazonSesOptions _options;

    public AmazonSesNotification(AmazonSesOptions options)
    {
        _options = options;
    }

    public async Task SendAsync(IEmailMessage emailMessage, CancellationToken cancellationToken = default)
    {
        var client = _options.CreateAmazonSimpleEmailServiceClient();

        var sendRequest = new SendEmailRequest
        {
            Source = emailMessage.From,
            Destination = new Destination
            {
                ToAddresses = emailMessage.Tos?.Split(';').Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToList(),
            },
            Message = new Message
            {
                Subject = new Content(emailMessage.Subject),
                Body = new Body
                {
                    Html = new Content
                    {
                        Charset = "UTF-8",
                        Data = emailMessage.Body,
                    },
                    Text = new Content
                    {
                        Charset = "UTF-8",
                        Data = emailMessage.Body,
                    },
                },
            },
        };

        var sendEmailResponse = await client.SendEmailAsync(sendRequest);

        if (sendEmailResponse?.HttpStatusCode == HttpStatusCode.OK && !string.IsNullOrWhiteSpace(sendEmailResponse?.MessageId))
        {
            // Succeeded
        }
        else
        {
            // Failed
        }
    }
}
