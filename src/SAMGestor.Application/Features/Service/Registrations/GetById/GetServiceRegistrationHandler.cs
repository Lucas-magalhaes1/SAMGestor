using MediatR;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Service.Registrations.GetById;

public sealed class GetServiceRegistrationHandler(
    IServiceRegistrationRepository regRepo,
    IServiceSpaceRepository spaceRepo
) : IRequestHandler<GetServiceRegistrationQuery, GetServiceRegistrationResponse>
{
    public async Task<GetServiceRegistrationResponse> Handle(
        GetServiceRegistrationQuery q, CancellationToken ct)
    {
        var reg = await regRepo.GetByIdAsync(q.RegistrationId, ct)
                  ?? throw new NotFoundException(nameof(ServiceRegistration), q.RegistrationId);

        if (reg.RetreatId != q.RetreatId)
            throw new NotFoundException(nameof(ServiceRegistration), q.RegistrationId);

        // Preferred space (nome)
        PreferredSpaceView? pref = null;
        if (reg.PreferredSpaceId is Guid sid)
        {
            var s = await spaceRepo.GetByIdAsync(sid, ct);
            if (s is not null)
                pref = new PreferredSpaceView(s.Id, s.Name);
        }

        return new GetServiceRegistrationResponse(
            Id: reg.Id,
            RetreatId: reg.RetreatId,
            FullName: (string)reg.Name,
            Cpf: reg.Cpf.Value,
            Email: reg.Email.Value,
            Phone: reg.Phone,
            BirthDate: reg.BirthDate,
            Gender: reg.Gender,
            City: reg.City,
            Region: reg.Region,
            PhotoUrl: reg.PhotoUrl?.Value,
            Status: reg.Status,
            Enabled: reg.Enabled,
            RegistrationDateUtc: reg.RegistrationDate,
            PreferredSpace: pref
        );
    }
}