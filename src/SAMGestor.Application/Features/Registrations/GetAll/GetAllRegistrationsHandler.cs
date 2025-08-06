using MediatR;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Registrations.GetAll;

public sealed class GetAllRegistrationsHandler(IRegistrationRepository repo)
    : IRequestHandler<GetAllRegistrationsQuery, GetAllRegistrationsResponse>
{
    public async Task<GetAllRegistrationsResponse> Handle(
        GetAllRegistrationsQuery query,
        CancellationToken ct)
    {
        var total = await repo.CountAsync(query.retreatId, query.status, query.region, ct);
        var registrations = await repo.ListAsync(query.retreatId, query.status, query.region, query.skip, query.take, ct);

        var items = registrations.Select(r => new RegistrationDto(
            r.Id,
            (string)r.Name,
            r.Cpf.Value,
            r.Status.ToString(),
            r.Region,
            r.ParticipationCategory.ToString(),
            r.RegistrationDate
        )).ToList();

        return new GetAllRegistrationsResponse(items, total, query.skip, query.take);
    }
}