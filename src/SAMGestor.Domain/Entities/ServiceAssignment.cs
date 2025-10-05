using SAMGestor.Domain.Commom;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Domain.Entities;

public class ServiceAssignment : Entity<Guid>
{
    public Guid        ServiceSpaceId        { get; private set; }
    public Guid        ServiceRegistrationId { get; private set; }
    public ServiceRole Role                  { get; private set; }
    public DateTimeOffset AssignedAt         { get; private set; }
    public Guid?       AssignedBy            { get; private set; } 

    private ServiceAssignment() { }

    public ServiceAssignment(Guid serviceSpaceId, Guid serviceRegistrationId, ServiceRole role, Guid? assignedBy = null)
    {
        Id                    = Guid.NewGuid();
        ServiceSpaceId        = serviceSpaceId;
        ServiceRegistrationId = serviceRegistrationId;
        Role                  = role;
        AssignedBy            = assignedBy;
        AssignedAt            = DateTimeOffset.UtcNow;
    }

    public void ChangeRole(ServiceRole newRole, Guid? assignedBy = null)
    {
        Role       = newRole;
        AssignedBy = assignedBy;
        AssignedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateTarget(Guid newServiceSpaceId, ServiceRole newRole, Guid? assignedBy = null)
    {
        ServiceSpaceId = newServiceSpaceId;
        Role           = newRole;
        AssignedBy     = assignedBy;
        AssignedAt     = DateTimeOffset.UtcNow;
    }

}