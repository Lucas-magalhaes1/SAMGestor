using FluentValidation;

namespace SAMGestor.Application.Features.Tents.Locking;

public sealed class SetTentLockValidator : AbstractValidator<SetTentLockCommand>
{
    public SetTentLockValidator()
    {
        RuleFor(x => x.RetreatId).NotEmpty();
        RuleFor(x => x.TentId).NotEmpty();
    }
}