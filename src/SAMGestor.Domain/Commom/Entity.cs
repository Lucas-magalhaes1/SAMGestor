namespace SAMGestor.Domain.Commom;

public abstract class Entity<TId>
{
    public TId Id { get; protected set; } = default!;   
}