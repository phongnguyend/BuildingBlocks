﻿using System.Linq;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace DddDotNet.Infrastructure.Notification.Email.Smtp;

public class SmtpEmailNotification : IEmailNotification
{
    private readonly SmtpOptions _options;

    public SmtpEmailNotification(SmtpOptions options)
    {
        _options = options;
    }

    public async Task SendAsync(IEmailMessage emailMessage, CancellationToken cancellationToken = default)
    {
        var mail = new MailMessage
        {
            From = new MailAddress(emailMessage.From, emailMessage.FromName),
        };

        emailMessage.Tos?.Split(';')
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList()
            .ForEach(x => mail.To.Add(x));

        emailMessage.CCs?.Split(';')
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList()
            .ForEach(x => mail.CC.Add(x));

        emailMessage.BCCs?.Split(';')
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList()
            .ForEach(x => mail.Bcc.Add(x));

        mail.Subject = emailMessage.Subject;

        mail.Body = emailMessage.Body;

        mail.IsBodyHtml = true;

        if (emailMessage.Attachments != null)
        {
            foreach (var attachment in emailMessage.Attachments)
            {
                mail.Attachments.Add(new System.Net.Mail.Attachment(attachment.Content, attachment.FileName));
            }
        }

        var smtpClient = new SmtpClient(_options.Host);

        if (_options.Port.HasValue)
        {
            smtpClient.Port = _options.Port.Value;
        }

        if (!string.IsNullOrWhiteSpace(_options.UserName) && !string.IsNullOrWhiteSpace(_options.Password))
        {
            smtpClient.Credentials = new System.Net.NetworkCredential(_options.UserName, _options.Password);
        }

        if (_options.EnableSsl.HasValue)
        {
            smtpClient.EnableSsl = _options.EnableSsl.Value;
        }

        await smtpClient.SendMailAsync(mail, cancellationToken);
    }
}
