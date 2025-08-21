namespace DddDotNet.Infrastructure.Notification.Email.MailKit;

public class MailKitOptions
{
    public string Host { get; set; }

    public int? Port { get; set; }

    public string UserName { get; set; }

    public string Password { get; set; }

    public bool? EnableSsl { get; set; }
}