using MediatR;
using SAMGestor.Domain.Interfaces;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace SAMGestor.Application.Features.Registrations.GetById;

public sealed class GetRegistrationByIdHandler(
    IRegistrationRepository regRepo,
    IFamilyMemberRepository familyMemberRepo,
    IFamilyRepository familyRepo
) : IRequestHandler<GetRegistrationByIdQuery, GetRegistrationByIdResponse?>
{
    public async Task<GetRegistrationByIdResponse?> Handle(GetRegistrationByIdQuery q, CancellationToken ct)
    {
        var r = await regRepo.GetByIdAsync(q.RegistrationId, ct);
        if (r is null) return null;

        FamilyMembershipDto? familyDto = null;

        var link = await familyMemberRepo.GetByRegistrationIdAsync(r.RetreatId, r.Id, ct);
        if (link is not null)
        {
            var fam = await familyRepo.GetByIdAsync(link.FamilyId, ct);
            if (fam is not null)
            {
                familyDto = new FamilyMembershipDto(
                    fam.Id,
                    (string)fam.Name,   // FamilyName (VO/converter)
                    link.Position
                );
            }
        }

        var birthIso = r.BirthDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var photo = r.PhotoUrl is null ? null : r.PhotoUrl.Value;

        return new GetRegistrationByIdResponse(
            r.Id,
            (string)r.Name,
            r.Cpf.Value,
            r.Email.Value,
            r.Phone,
            r.City,
            r.Gender.ToString(),
            r.Status.ToString(),
            r.ParticipationCategory.ToString(),
            r.Enabled,
            r.Region,
            r.RetreatId,
            r.TentId,
            r.TeamId,
            birthIso,
            photo,
            r.CompletedRetreat,
            r.RegistrationDate,
            familyDto
        );
    }
}