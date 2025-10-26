    using MediatR;

    namespace SAMGestor.Application.Features.Tents.TentRoster.Assign;

    public sealed record UpdateTentRosterCommand(
        Guid RetreatId,
        int  Version,                                   // Retreat.TentsVersion (controle de concorrência)
        IReadOnlyList<TentRosterSnapshot> Tents         // snapshot por barraca
    ) : IRequest<UpdateTentRosterResponse>;

    public sealed record TentRosterSnapshot(
        Guid TentId,
        IReadOnlyList<TentRosterMemberItem> Members     // RegistrationId + Position
    );

    public sealed record TentRosterMemberItem(
        Guid RegistrationId,
        int  Position
    );