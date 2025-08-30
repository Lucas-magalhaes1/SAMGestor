namespace SAMGestor.Payment.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqOptions
{
    public string HostName { get; set; } = "rabbitmq";
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Exchange { get; set; } = "sam.topic";
}