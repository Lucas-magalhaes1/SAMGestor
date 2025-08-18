namespace SAMGestor.Notification.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public string HostName { get; set; } = "rabbitmq";
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Exchange { get; set; } = "sam.topic";
    public string SelectionQueue { get; set; } = "notification.selection";
    public string SelectionRoutingKey { get; set; } = "selection.participant.selected";
}