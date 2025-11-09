using FluentValidation;
using MediatR;
using SAMGestor.Application.Dtos.Dashboards;
using SAMGestor.Application.Interfaces.Reports;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Dashboards.Overview;

public sealed record GetOverviewQuery(
    Guid RetreatId,
    DateOnly? From = null,
    DateOnly? To = null
) : IRequest<DashboardOverviewDto>;

public sealed class GetOverviewValidator : AbstractValidator<GetOverviewQuery>
{
    public GetOverviewValidator()
    {
        RuleFor(x => x.RetreatId).NotEmpty();
        RuleFor(x => x.To)
            .Must((q, to) => to is null || q.From is null || to >= q.From)
            .WithMessage("'to' deve ser maior/igual a 'from'.");
    }
}

public sealed class GetOverviewHandler : IRequestHandler<GetOverviewQuery, DashboardOverviewDto>
{
    private readonly IReportingReadDb _db;
    public GetOverviewHandler(IReportingReadDb db) => _db = db;

    public async Task<DashboardOverviewDto> Handle(GetOverviewQuery request, CancellationToken ct)
    {
        var rid = request.RetreatId;

        // ---------- FAZER (participantes) ----------
        var regs = await _db.ToListAsync(
            _db.AsNoTracking().Registrations
               .Where(r => r.Status == RegistrationStatus.Confirmed && r.RetreatId == rid)
               .Select(r => new
               {
                   r.Id,
                   r.Gender,
                   r.City,
                   r.ShirtSize,
                   r.BirthDate,
                   r.RegistrationDate
               }), ct);

        var regIds = regs.Select(r => r.Id).ToHashSet();

        var pays = await _db.ToListAsync(
            _db.AsNoTracking().Payments
               .Select(p => new { p.Id, p.RegistrationId, p.Status, p.Method, p.PaidAt }), ct);

        var paysFazer = pays.Where(p => p.RegistrationId != Guid.Empty && regIds.Contains(p.RegistrationId)).ToList();
        var paidSetFazer = paysFazer.Where(p => p.Status == PaymentStatus.Paid).Select(p => p.RegistrationId).ToHashSet();

        var tents = await _db.ToListAsync(
            _db.AsNoTracking().Tents
               .Where(t => t.RetreatId == rid)
               .Select(t => new { t.Id, t.Capacity, t.IsActive }), ct);

        var tentIds = tents.Select(t => t.Id).ToHashSet();
        var capacity = tents.Where(t => t.IsActive).Sum(t => t.Capacity);

        var assigns = await _db.ToListAsync(
            _db.AsNoTracking().TentAssignments
               .Select(a => new { a.TentId, a.RegistrationId }), ct);

        var assigned = assigns.Count(a => tentIds.Contains(a.TentId));

        var totalConfirmed = regs.Count;
        var totalPaid      = paidSetFazer.Count;
        var totalPending   = totalConfirmed - totalPaid;
        var occupancyPct   = capacity > 0 ? (double)assigned / capacity * 100.0 : 0.0;

        var maleCount   = regs.Count(r => r.Gender == Gender.Male);
        var femaleCount = regs.Count(r => r.Gender == Gender.Female);
        var denom       = maleCount + femaleCount;
        var malePct     = denom > 0 ? (double)maleCount / denom * 100.0 : 0.0;
        var femalePct   = denom > 0 ? (double)femaleCount / denom * 100.0 : 0.0;

        var shirts = regs
           .Where(r => r.ShirtSize.HasValue && (int)r.ShirtSize.Value != 0 && paidSetFazer.Contains(r.Id))
           .GroupBy(r => MapSize(r.ShirtSize!.Value))
           .Select(g => new BreakdownItemDto { Label = g.Key, Value = g.Count() })
           .OrderBy(x => x.Label)
           .ToArray();

        var citiesTop = regs
           .Where(r => !string.IsNullOrWhiteSpace(r.City))
           .GroupBy(r => r.City)
           .Select(g => new BreakdownItemDto { Label = g.Key, Value = g.Count() })
           .OrderByDescending(x => x.Value)
           .Take(5)
           .ToArray();

        var famMembers = await _db.ToListAsync(
            _db.AsNoTracking().FamilyMembers.Select(f => new { f.FamilyId, f.RegistrationId }), ct);
        var families = await _db.ToListAsync(
            _db.AsNoTracking().Families.Select(f => new { f.Id, Name = f.Name.Value }), ct);
        var famById = families.ToDictionary(x => x.Id, x => x.Name);

        var famAgg = regs
            .GroupBy(r =>
            {
                var famId = famMembers.FirstOrDefault(x => x.RegistrationId == r.Id)?.FamilyId;
                return (famId.HasValue && famById.TryGetValue(famId.Value, out var n)) ? n : "Sem Família";
            })
            .Select(g => new
            {
                Family = g.Key,
                Paid   = g.Count(r => paidSetFazer.Contains(r.Id))
            })
            .OrderByDescending(x => x.Paid)
            .Take(5)
            .Select(x => new BreakdownItemDto { Label = x.Family, Value = x.Paid })
            .ToArray();

        var byMethod = paysFazer
            .Where(p => p.Status == PaymentStatus.Paid)
            .GroupBy(p => p.Method.ToString())
            .Select(g => new BreakdownItemDto { Label = g.Key, Value = g.Count() })
            .OrderByDescending(x => x.Value)
            .ToArray();

        var (from, to) = ResolveRange(regs.Select(r => DateOnly.FromDateTime(r.RegistrationDate)),
                                      paysFazer.Where(p => p.Status == PaymentStatus.Paid).Select(p => p.PaidAt),
                                      request.From, request.To);

        var days = DatesBetween(from, to);
        var paidByDay = paysFazer
            .Where(p => p.Status == PaymentStatus.Paid && p.PaidAt.HasValue)
            .GroupBy(p => DateOnly.FromDateTime(p.PaidAt!.Value))
            .ToDictionary(g => g.Key, g => g.Count());

        var pendingByDay = regs
            .Where(r => !paidSetFazer.Contains(r.Id))
            .GroupBy(r => DateOnly.FromDateTime(r.RegistrationDate))
            .ToDictionary(g => g.Key, g => g.Count());

        var series = days
            .Select(d => new PaymentPointDto
            {
                Date = d.ToString("yyyy-MM-dd"),
                Paid = paidByDay.TryGetValue(d, out var pcount) ? pcount : 0,
                Pending = pendingByDay.TryGetValue(d, out var qcount) ? qcount : 0
            })
            .ToArray();

        // ---------- SERVIÇO ----------
        // spaces do retiro
        var spaces = await _db.ToListAsync(
            _db.AsNoTracking().ServiceSpaces
               .Where(s => s.RetreatId == rid && s.IsActive)
               .Select(s => new { s.Id, s.Name, s.MaxPeople }), ct);
        var spaceIds = spaces.Select(s => s.Id).ToHashSet();

        // registrations de serviço do retiro
        var sregs = await _db.ToListAsync(
            _db.AsNoTracking().ServiceRegistrations
               .Where(sr => sr.RetreatId == rid && sr.Enabled)
               .Select(sr => new { sr.Id, sr.Status }), ct);
        var sregIds = sregs.Select(x => x.Id).ToHashSet();

        // assignments (alocações)
        var sassigns = await _db.ToListAsync(
            _db.AsNoTracking().ServiceAssignments
               .Select(a => new { a.ServiceSpaceId, a.ServiceRegistrationId }), ct);
        var sassignsRetreat = sassigns.Where(a => spaceIds.Contains(a.ServiceSpaceId)
                                              && sregIds.Contains(a.ServiceRegistrationId))
                                      .ToList();

        // pagamentos de serviço: ServiceRegistrationPayment -> Payment(Paid)
        var link = await _db.ToListAsync(
            _db.AsNoTracking().ServiceRegistrationPayments
               .Select(x => new { x.ServiceRegistrationId, x.PaymentId }), ct);

        var payById = pays.ToDictionary(p => p.Id, p => p); // já carregado lá em cima
        var paidServiceSet = new HashSet<Guid>();
        foreach (var l in link)
        {
            if (!sregIds.Contains(l.ServiceRegistrationId)) continue;
            if (payById.TryGetValue(l.PaymentId, out var pay) && pay.Status == PaymentStatus.Paid)
                paidServiceSet.Add(l.ServiceRegistrationId);
        }

        // KPIs de serviço
        var submitted = sregs.Count; // todos habilitados do retiro
        var confirmed = sregs.Count(x => x.Status == ServiceRegistrationStatus.Confirmed);
        var declined  = sregs.Count(x => x.Status == ServiceRegistrationStatus.Declined);
        var cancelled = sregs.Count(x => x.Status == ServiceRegistrationStatus.Cancelled);
        var assignedService = sassignsRetreat.Select(a => a.ServiceRegistrationId).Distinct().Count();
        var paidService = paidServiceSet.Count;

        // Por espaço
        var bySpace = spaces
            .Select(sp =>
            {
                var regsForSpaceSubmitted = sregs.Count; // "submitted" é geral do retiro (não por espaço)
                // Confirmed por espaço (considera PreferredSpaceId? – sem relação direta, usamos assignments para ocupação)
                var assignedCount = sassignsRetreat.Count(a => a.ServiceSpaceId == sp.Id);
                var confirmedCount = sregs.Count(x => x.Status == ServiceRegistrationStatus.Confirmed); // se quiser por espaço: precisaria de relação; sem ela, manter geral
                var occ = sp.MaxPeople > 0 ? (double)assignedCount / sp.MaxPeople * 100.0 : 0.0;

                return new ServiceSpaceItemDto
                {
                    Label = sp.Name,
                    Capacity = sp.MaxPeople,
                    Submitted = regsForSpaceSubmitted, // métrica global por enquanto (pode ficar mais útil quando houver relação PreferredSpaceId/Team)
                    Confirmed = confirmedCount,        // idem
                    Assigned = assignedCount,
                    OccupancyPercent = Math.Round(occ, 1)
                };
            })
            .OrderByDescending(x => x.Assigned)
            .ToArray();

        var serviceDto = new OverviewServiceDto
        {
            Kpis = new ServiceKpisDto
            {
                Submitted = submitted,
                Confirmed = confirmed,
                Declined  = declined,
                Cancelled = cancelled,
                Assigned  = assignedService,
                Paid      = paidService
            },
            Spaces = bySpace
        };

        // ---------- retorno ----------
        var retreatHeader = new OverviewRetreatDto
        {
            Id = rid,
            Name = "Retiro",
            Edition = ""
        };

        return new DashboardOverviewDto
        {
            Retreat = retreatHeader,
            Kpis = new OverviewKpisDto
            {
                TotalConfirmed   = totalConfirmed,
                TotalPaid        = totalPaid,
                TotalPending     = totalPending,
                Capacity         = capacity,
                OccupancyPercent = Math.Round(occupancyPct, 1)
            },
            Gender = new OverviewGenderDto
            {
                Male   = Math.Round(malePct, 1),
                Female = Math.Round(femalePct, 1)
            },
            Shirts = shirts,
            CitiesTop = citiesTop,
            Families = new OverviewFamiliesDto
            {
                Count = families.Count,
                TopByPaid = famAgg
            },
            Tents = new OverviewTentsDto
            {
                Total = tents.Count,
                Occupied = assigned,
                OccupancyPercent = Math.Round(occupancyPct, 1)
            },
            Payments = new OverviewPaymentsDto
            {
                ByMethod = byMethod,
                TimeSeries = series
            },
            Service = serviceDto
        };
    }

    private static string MapSize(ShirtSize s) => s switch
    {
        ShirtSize.P   => "P",
        ShirtSize.M   => "M",
        ShirtSize.G   => "G",
        ShirtSize.GG  => "GG",
        ShirtSize.GG1 => "GG1",
        ShirtSize.GG2 => "GG2",
        ShirtSize.GG3 => "GG3",
        ShirtSize.GG4 => "GG4",
        _ => "—"
    };

    private static (DateOnly from, DateOnly to) ResolveRange(
        IEnumerable<DateOnly> regDates,
        IEnumerable<DateTime?> paidDates,
        DateOnly? fromReq,
        DateOnly? toReq)
    {
        if (fromReq.HasValue && toReq.HasValue) return (fromReq.Value, toReq.Value);

        var minReg = regDates.DefaultIfEmpty(DateOnly.FromDateTime(DateTime.UtcNow)).Min();
        var minPay = paidDates.Where(d => d.HasValue).Select(d => DateOnly.FromDateTime(d!.Value)).DefaultIfEmpty(minReg).Min();
        var from = fromReq ?? (minReg < minPay ? minReg : minPay);
        var to = toReq ?? DateOnly.FromDateTime(DateTime.UtcNow);
        if (to < from) to = from;
        return (from, to);
    }

    private static IEnumerable<DateOnly> DatesBetween(DateOnly from, DateOnly to)
    {
        for (var d = from; d <= to; d = d.AddDays(1))
            yield return d;
    }
}
