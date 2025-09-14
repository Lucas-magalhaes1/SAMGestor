using SAMGestor.Domain.Commom;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Domain.Entities;

public class Family : Entity<Guid>
{
    private readonly List<FamilyMember> _members = new();

    public FamilyName Name { get; private set; } 
    public Guid     RetreatId  { get; private set; }
    
    public bool IsLocked { get; private set; } 

    /// <summary>Capacidade máxima de membros (p/ MVP: 4).</summary>
    public int Capacity   { get; private set; }

    /// <summary>Controle de concorrência das famílias do retiro (mantido em nível de Retreat; aqui deixa simples).</summary>
    public IReadOnlyCollection<FamilyMember> Members => _members;

    private Family() { }

    public Family(FamilyName name, Guid retreatId, int capacity)
    {
        Id        = Guid.NewGuid();
        Name      = name;
        RetreatId = retreatId;
        Capacity  = capacity;
    }

    public void Rename(FamilyName name) => Name = name;

    public void SetCapacity(int capacity)
    {
        if (capacity <= 0) throw new ArgumentException(nameof(capacity));
        Capacity = capacity;
    }
    
    public void Lock()   => IsLocked = true;
    public void Unlock() => IsLocked = false;
}