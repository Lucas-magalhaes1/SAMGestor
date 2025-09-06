using SAMGestor.Domain.Commom;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Domain.Entities;

public class Family : Entity<Guid>
{
    private readonly List<FamilyMember> _members = new();

    public FullName Name   { get; private set; }
    public Guid     RetreatId  { get; private set; }

    /// <summary>Capacidade máxima de membros (p/ MVP: 4).</summary>
    public int Capacity   { get; private set; }

    /// <summary>Controle de concorrência das famílias do retiro (mantido em nível de Retreat; aqui deixa simples).</summary>
    public IReadOnlyCollection<FamilyMember> Members => _members;

    private Family() { }

    public Family(FullName name, Guid retreatId, int capacity)
    {
        Id        = Guid.NewGuid();
        Name      = name;
        RetreatId = retreatId;
        Capacity  = capacity;
    }

    public void Rename(FullName name) => Name = name;

    public void SetCapacity(int capacity)
    {
        if (capacity <= 0) throw new ArgumentException(nameof(capacity));
        Capacity = capacity;
    }
}