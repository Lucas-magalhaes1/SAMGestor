namespace SAMGestor.Domain.Interfaces;

public interface IRelationshipService
{
    bool AreDirectRelatives(Guid registrationId1, Guid registrationId2);
    bool AreSpouses(Guid registrationId1, Guid registrationId2);
}