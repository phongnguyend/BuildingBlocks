using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Notification.Email.MailKit;

public class MailKitEmailNotification : IEmailNotification
{
    private readonly MailKitOptions _options;

    public MailKitEmailNotification(MailKitOptions options)
    {
        _options = options;
    }

    public async Task SendAsync(IEmailMessage emailMessage, CancellationToken cancellationToken = default)
    {
        var message = new MimeMessage();

        // Set From address
        message.From.Add(new MailboxAddress(emailMessage.FromName ?? emailMessage.From, emailMessage.From));

        // Add To addresses
        emailMessage.Tos?.Split(';')
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToList()
            .ForEach(x => message.To.Add(new MailboxAddress(string.Empty, x)));

        // Add CC addresses
        emailMessage.CCs?.Split(';')
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToList()
            .ForEach(x => message.Cc.Add(new MailboxAddress(string.Empty, x)));

        // Add BCC addresses
        emailMessage.BCCs?.Split(';')
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToList()
            .ForEach(x => message.Bcc.Add(new MailboxAddress(string.Empty, x)));

        // Set Subject
        message.Subject = emailMessage.Subject;

        // Create body builder
        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = emailMessage.Body
        };

        // Add attachments
        if (emailMessage.Attachments != null)
        {
            foreach (var attachment in emailMessage.Attachments)
            {
                bodyBuilder.Attachments.Add(attachment.FileName, attachment.Content);
            }
        }

        message.Body = bodyBuilder.ToMessageBody();

        // Send the email
        using var client = new SmtpClient();

        // Configure SSL/TLS
        var secureSocketOptions = SecureSocketOptions.Auto;
        if (_options.EnableSsl.HasValue)
        {
            secureSocketOptions = _options.EnableSsl.Value ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None;
        }

        // Connect to the SMTP server
        var port = _options.Port ?? 587; // Default to 587 if not specified
        await client.ConnectAsync(_options.Host, port, secureSocketOptions, cancellationToken);

        // Authenticate if credentials are provided
        if (!string.IsNullOrWhiteSpace(_options.UserName) && !string.IsNullOrWhiteSpace(_options.Password))
        {
            await client.AuthenticateAsync(_options.UserName, _options.Password, cancellationToken);
        }

        // Send the message
        await client.SendAsync(message, cancellationToken);

        // Disconnect
        await client.DisconnectAsync(true, cancellationToken);
    }
}