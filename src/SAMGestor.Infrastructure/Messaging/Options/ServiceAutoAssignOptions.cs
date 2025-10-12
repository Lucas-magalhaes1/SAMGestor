namespace SAMGestor.Infrastructure.Messaging.Options;

public sealed class ServiceAutoAssignOptions
{
    /// <summary>Ativa/desativa o auto-assign pós-pagamento.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Se true, tenta respeitar uma capacidade máxima do espaço (se existir propriedade Max/MaxCapacity/MaxSlots etc.).</summary>
    public bool EnforceMax { get; set; } = false;
}   