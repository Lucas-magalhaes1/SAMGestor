using MediatR;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Tents.TentRoster.Unassigned;

public sealed class GetTentUnassignedHandler(
    IRegistrationRepository regRepo
) : IRequestHandler<GetTentUnassignedQuery, GetTentUnassignedResponse>
{
    public async Task<GetTentUnassignedResponse> Handle(GetTentUnassignedQuery q, CancellationToken ct)
    {
        Gender? g = null;
        if (!string.IsNullOrWhiteSpace(q.Gender)
            && Enum.TryParse<Gender>(q.Gender, true, out var parsed))
        {
            g = parsed;
        }

        var regs = await regRepo.ListPaidUnassignedAsync(q.RetreatId, g, q.Search, ct);

        var items = regs
            .OrderBy(r => r.Gender) 
            .ThenBy(r => r.Name.Value)
            .Select(r => new UnassignedMemberView(
                r.Id,
                (string)r.Name,
                r.Gender.ToString(),
                r.City
            ))
            .ToList();

        return new GetTentUnassignedResponse(q.RetreatId, items);
    }
}