using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Domain.Interfaces;

public interface ITeamRepository
{
    Team? GetById(Guid id);
    int  GetOccupancy(Guid teamId);                       // membros atuais
    bool HasRole(Guid teamId, TeamMemberRole role);       // existe alguém com esse papel?
}