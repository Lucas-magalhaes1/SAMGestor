namespace SAMGestor.Domain.Interfaces;

public interface IWaitingListRepository
{
    bool Exists(Guid registrationId, Guid retreatId);
    int  CountByRetreat(Guid retreatId);
}