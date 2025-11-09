using FluentValidation;
using MediatR;
using SAMGestor.Application.Dtos.Dashboards;
using SAMGestor.Application.Interfaces.Reports;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Dashboards.Service;

public sealed record GetServiceOverviewQuery(Guid RetreatId) : IRequest<OverviewServiceDto>;

public sealed class GetServiceOverviewValidator : AbstractValidator<GetServiceOverviewQuery>
{
    public GetServiceOverviewValidator()
    {
        RuleFor(x => x.RetreatId).NotEmpty();
    }
}

public sealed class GetServiceOverviewHandler : IRequestHandler<GetServiceOverviewQuery, OverviewServiceDto>
{
    private readonly IReportingReadDb _db;
    public GetServiceOverviewHandler(IReportingReadDb db) => _db = db;

    public async Task<OverviewServiceDto> Handle(GetServiceOverviewQuery request, CancellationToken ct)
    {
        var rid = request.RetreatId;

        var spaces = await _db.ToListAsync(
            _db.AsNoTracking().ServiceSpaces
               .Where(s => s.RetreatId == rid && s.IsActive)
               .Select(s => new { s.Id, s.Name, s.MaxPeople }), ct);
        var spaceIds = spaces.Select(s => s.Id).ToHashSet();

        var sregs = await _db.ToListAsync(
            _db.AsNoTracking().ServiceRegistrations
               .Where(sr => sr.RetreatId == rid && sr.Enabled)
               .Select(sr => new { sr.Id, sr.Status, sr.PreferredSpaceId }), ct);
        var sregIds = sregs.Select(x => x.Id).ToHashSet();

        var sassigns = await _db.ToListAsync(
            _db.AsNoTracking().ServiceAssignments
               .Select(a => new { a.ServiceSpaceId, a.ServiceRegistrationId }), ct);
        var sassignsRetreat = sassigns.Where(a => spaceIds.Contains(a.ServiceSpaceId)
                                              && sregIds.Contains(a.ServiceRegistrationId))
                                      .ToList();

        var pays = await _db.ToListAsync(
            _db.AsNoTracking().Payments
               .Select(p => new { p.Id, p.Status }), ct);

        var link = await _db.ToListAsync(
            _db.AsNoTracking().ServiceRegistrationPayments
               .Select(x => new { x.ServiceRegistrationId, x.PaymentId }), ct);

        var payById = pays.ToDictionary(p => p.Id, p => p);
        var paidServiceSet = new HashSet<Guid>();
        foreach (var l in link)
            if (sregIds.Contains(l.ServiceRegistrationId)
                && payById.TryGetValue(l.PaymentId, out var pay)
                && pay.Status == PaymentStatus.Paid)
                paidServiceSet.Add(l.ServiceRegistrationId);

        var submitted = sregs.Count;
        var confirmed = sregs.Count(x => x.Status == ServiceRegistrationStatus.Confirmed);
        var declined  = sregs.Count(x => x.Status == ServiceRegistrationStatus.Declined);
        var cancelled = sregs.Count(x => x.Status == ServiceRegistrationStatus.Cancelled);
        var assigned  = sassignsRetreat.Select(a => a.ServiceRegistrationId).Distinct().Count();
        var paid      = paidServiceSet.Count;

        var bySpace = spaces
            .Select(sp =>
            {
                var assignedCount = sassignsRetreat.Count(a => a.ServiceSpaceId == sp.Id);
                var occ = sp.MaxPeople > 0 ? (double)assignedCount / sp.MaxPeople * 100.0 : 0.0;

                return new ServiceSpaceItemDto
                {
                    Label = sp.Name,
                    Capacity = sp.MaxPeople,
                    Submitted = submitted, // global (até ter confirmação por espaço)
                    Confirmed = confirmed, // global
                    Assigned = assignedCount,
                    OccupancyPercent = Math.Round(occ, 1)
                };
            })
            .OrderByDescending(x => x.Assigned)
            .ToArray();

        return new OverviewServiceDto
        {
            Kpis = new ServiceKpisDto
            {
                Submitted = submitted,
                Confirmed = confirmed,
                Declined  = declined,
                Cancelled = cancelled,
                Assigned  = assigned,
                Paid      = paid
            },
            Spaces = bySpace
        };
    }
}
