using FluentValidation;
using MediatR;
using SAMGestor.Application.Common.Pagination;
using SAMGestor.Application.Dtos.Dashboards;

namespace SAMGestor.Application.Features.Dashboards.Families;

public sealed record GetFamiliesQuery(
    Guid RetreatId, 
    int Skip = 0, 
    int Take = 10
) : IRequest<PagedResult<FamilyRowDto>>;

public sealed class GetFamiliesValidator : AbstractValidator<GetFamiliesQuery>
{
    public GetFamiliesValidator()
    {
        RuleFor(x => x.RetreatId).NotEmpty();
        RuleFor(x => x.Skip).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Take).GreaterThanOrEqualTo(0).LessThanOrEqualTo(1000);
    }
}