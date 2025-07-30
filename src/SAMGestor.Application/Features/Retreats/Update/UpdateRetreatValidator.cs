using FluentValidation;
using SAMGestor.Application.Common.Retreat;
using SAMGestor.Application.Features.Retreats.Update;

namespace SAMGestor.Application.Features.Retreats.Update;

public class UpdateRetreatValidator 
    : BaseRetreatValidator<UpdateRetreatCommand>
{
    public UpdateRetreatValidator()
    {
        RuleFor(x => x.Id).NotEmpty();   
    }
}