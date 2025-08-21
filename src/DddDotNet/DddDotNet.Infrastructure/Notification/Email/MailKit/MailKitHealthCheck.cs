using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MimeKit;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Notification.Email.MailKit;

public class MailKitHealthCheck : IHealthCheck
{
    private readonly MailKitHealthCheckOptions _options;

    public MailKitHealthCheck(MailKitHealthCheckOptions options)
    {
        _options = options;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new MimeMessage();

            // Set From address
            message.From.Add(new MailboxAddress(_options.FromName ?? _options.From, _options.From));

            // Add To addresses
            _options.Tos?.Split(';')
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList()
                .ForEach(x => message.To.Add(new MailboxAddress("", x)));

            // Add CC addresses
            _options.CCs?.Split(';')
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList()
                .ForEach(x => message.Cc.Add(new MailboxAddress("", x)));

            // Add BCC addresses
            _options.BCCs?.Split(';')
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList()
                .ForEach(x => message.Bcc.Add(new MailboxAddress("", x)));

            // Set Subject and Body
            message.Subject = _options.Subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = _options.Body
            };

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

            return HealthCheckResult.Healthy();
        }
        catch (Exception exception)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, null, exception);
        }
    }
}

public class MailKitHealthCheckOptions : MailKitOptions
{
    public string From { get; set; }

    public string FromName { get; set; }

    public string Tos { get; set; }

    public string CCs { get; set; }

    public string BCCs { get; set; }

    public string Subject { get; set; }

    public string Body { get; set; }
}