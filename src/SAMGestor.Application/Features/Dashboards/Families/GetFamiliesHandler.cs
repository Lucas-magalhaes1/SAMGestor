using MediatR;
using SAMGestor.Application.Common.Pagination;
using SAMGestor.Application.Dtos.Dashboards;
using SAMGestor.Application.Interfaces.Reports;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Dashboards.Families;

public sealed class GetFamiliesHandler : IRequestHandler<GetFamiliesQuery, PagedResult<FamilyRowDto>>
{
    private readonly IReportingReadDb _db;
    public GetFamiliesHandler(IReportingReadDb db) => _db = db;

    public async Task<PagedResult<FamilyRowDto>> Handle(GetFamiliesQuery request, CancellationToken ct)
    {
        var rid = request.RetreatId;

        var regs = await _db.ToListAsync(
            _db.AsNoTracking().Registrations
               .Where(r => r.Status == RegistrationStatus.Confirmed && r.RetreatId == rid)
               .Select(r => new { r.Id, r.Gender, r.BirthDate }), ct);

        var regIds = regs.Select(r => r.Id).ToHashSet();

        var pays = await _db.ToListAsync(
            _db.AsNoTracking().Payments
               .Where(p => p.Status == PaymentStatus.Paid)
               .Select(p => new { p.RegistrationId }), ct);

        var paidSet = pays.Where(p => regIds.Contains(p.RegistrationId))
                          .Select(p => p.RegistrationId).ToHashSet();

        var famMembers = await _db.ToListAsync(
            _db.AsNoTracking().FamilyMembers.Select(f => new { f.FamilyId, f.RegistrationId }), ct);
        var families = await _db.ToListAsync(
            _db.AsNoTracking().Families.Select(f => new { f.Id, Name = f.Name.Value }), ct);
        var famById = families.ToDictionary(x => x.Id, x => x.Name);

        var now = DateOnly.FromDateTime(DateTime.UtcNow);

        var grouped = regs
            .GroupBy(r =>
            {
                var famId = famMembers.FirstOrDefault(x => x.RegistrationId == r.Id)?.FamilyId;
                return (famId.HasValue && famById.TryGetValue(famId.Value, out var n)) ? n : "Sem Família";
            })
            .Select(g =>
            {
                var list = g.ToList();
                var confirmed = list.Count;
                var paid     = list.Count(x => paidSet.Contains(x.Id));
                var pending  = confirmed - paid;
                var ages     = list.Select(x => AgeYears(x.BirthDate, now)).Where(a => a >= 0).ToArray();
                var avgAge   = ages.Length > 0 ? ages.Average() : 0;
                var female   = list.Count(x => x.Gender == Gender.Female);
                var femalePct = confirmed > 0 ? (double)female / confirmed * 100.0 : 0.0;

                return new FamilyRowDto
                {
                    Family = g.Key,
                    Confirmed = confirmed,
                    Paid = paid,
                    Pending = pending,
                    AvgAge = Math.Round(avgAge, 1),
                    FemalePercent = Math.Round(femalePct, 1)
                };
            })
            .OrderByDescending(x => x.Paid)
            .ThenBy(x => x.Family)
            .ToList();

        var totalCount = grouped.Count;

        var items = grouped
            .ApplyPagination(request.Skip, request.Take)
            .ToList();

        return new PagedResult<FamilyRowDto>(items, totalCount, request.Skip, request.Take);
    }

    private static int AgeYears(DateOnly birth, DateOnly refDate)
    {
        if (birth == default) return -1;
        var age = refDate.Year - birth.Year;
        if (birth.AddYears(age) > refDate) age--;
        return age;
    }
}
