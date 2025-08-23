namespace SAMGestor.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqOptions
{
    public string HostName { get; set; } = "rabbitmq";
    
    public int    Port     { get; set; } = 5672;  
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Exchange { get; set; } = "sam.topic";
}