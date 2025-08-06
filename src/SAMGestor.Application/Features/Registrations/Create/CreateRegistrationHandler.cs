    using MediatR;
    using SAMGestor.Application.Interfaces;
    using SAMGestor.Domain.Entities;
    using SAMGestor.Domain.Enums;
    using SAMGestor.Domain.Exceptions;
    using SAMGestor.Domain.Interfaces;

    namespace SAMGestor.Application.Features.Registrations.Create;

    public sealed class CreateRegistrationHandler(
        IRegistrationRepository regRepo,
        IRetreatRepository retRepo,
        IUnitOfWork uow)
        : IRequestHandler<CreateRegistrationCommand, CreateRegistrationResponse>
    {
        public async Task<CreateRegistrationResponse> Handle(
            CreateRegistrationCommand cmd,
            CancellationToken         ct)
        {
            /* 1. Retreat must exist and accept registrations */
            var retreat = await retRepo.GetByIdAsync(cmd.RetreatId, ct);
            if (retreat is null)
                throw new NotFoundException(nameof(Retreat), cmd.RetreatId);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (!retreat.RegistrationWindowOpen(today))
                throw new BusinessRuleException("Registration period closed.");

            /* 2. CPF not blocked */
            if (await regRepo.IsCpfBlockedAsync(cmd.Cpf, ct))
                throw new BusinessRuleException("CPF is blocked.");

            /* 3. CPF unique within this retreat */
            if (await regRepo.ExistsByCpfInRetreatAsync(cmd.Cpf, cmd.RetreatId, ct))
                throw new BusinessRuleException("CPF already registered for this retreat.");

            /* 4. Create entity */
            var reg = new Registration(
                cmd.Name,
                cmd.Cpf,
                cmd.Email,
                cmd.Phone,
                cmd.BirthDate,
                cmd.Gender,
                cmd.City,
                RegistrationStatus.NotSelected,
                cmd.ParticipationCategory,
                cmd.Region,
                cmd.RetreatId);

            await regRepo.AddAsync(reg, ct);
            await uow.SaveChangesAsync(ct);

            return new CreateRegistrationResponse(reg.Id);
        }
    }
