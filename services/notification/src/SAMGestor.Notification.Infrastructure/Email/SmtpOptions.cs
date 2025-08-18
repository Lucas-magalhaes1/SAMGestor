namespace SAMGestor.Notification.Infrastructure.Email;

public sealed class SmtpOptions
{
    public string Host { get; set; } = "mailhog";
    public int Port { get; set; } = 1025;
    public bool EnableSsl { get; set; } = false;
    public string FromAddress { get; set; } = "no-reply@samgestor.local";
    public string FromName { get; set; } = "SAMGestor";
}