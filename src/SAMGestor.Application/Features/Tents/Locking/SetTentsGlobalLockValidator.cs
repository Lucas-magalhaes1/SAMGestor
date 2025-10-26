using FluentValidation;

namespace SAMGestor.Application.Features.Tents.Locking;

public sealed class SetTentsGlobalLockValidator : AbstractValidator<SetTentsGlobalLockCommand>
{
    public SetTentsGlobalLockValidator()
    {
        RuleFor(x => x.RetreatId).NotEmpty();
    }
}