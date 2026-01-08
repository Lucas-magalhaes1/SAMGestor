using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Reports.Templates;

public sealed class ContemplatedByFamilyTemplate : IDescribedReportTemplate
{
    public const string TemplateKeyConst = "contemplated.by-family";
    public string Key => TemplateKeyConst;
    public string DefaultTitle => "Contemplados por Família (Pagos)";

    private readonly IReportingReadDb _readDb;
    public ContemplatedByFamilyTemplate(IReportingReadDb readDb) => _readDb = readDb;

    public ReportTemplateSchemaDto Describe() => new(
        Key,
        DefaultTitle,
        new[]
        {
            new ColumnDef("family", "Família"),
            new ColumnDef("participants", "Participantes"),
            new ColumnDef("count", "Qtde")
        },
        new[] { "totalFamilies", "totalParticipants" },
        SupportsPaging: true,
        DefaultPageLimit: 20
    );

    public async Task<ReportPayload> GetDataAsync(ReportContext ctx, int page, int pageLimit, CancellationToken ct)
    {
        var regsBase = _readDb.AsNoTracking().Registrations
            .Where(r => r.Status == RegistrationStatus.Confirmed);
        if (ctx.RetreatId.HasValue) regsBase = regsBase.Where(r => r.RetreatId == ctx.RetreatId.Value);

        var paidIds = await _readDb.ToListAsync(
            _readDb.AsNoTracking().Payments.Where(p => p.Status == PaymentStatus.Paid).Select(p => p.RegistrationId), ct);
        var paidSet = paidIds.ToHashSet();

        var regs = await _readDb.ToListAsync(
            regsBase.Where(r => paidSet.Contains(r.Id)).Select(r => new { r.Id, Name = r.Name.Value }), ct);

        var fm = await _readDb.ToListAsync(_readDb.AsNoTracking().FamilyMembers.Select(f => new { f.FamilyId, f.RegistrationId }), ct);
        var families = await _readDb.ToListAsync(_readDb.AsNoTracking().Families.Select(f => new { f.Id, Name = f.Name.Value }), ct);
        var famById = families.ToDictionary(x => x.Id, x => x.Name);

        var grouped = regs
            .GroupBy(r =>
            {
                var famId = fm.FirstOrDefault(x => x.RegistrationId == r.Id)?.FamilyId;
                return famId.HasValue && famById.TryGetValue(famId.Value, out var n) ? n : "Sem Família";
            })
            .Select(g => new { Family = g.Key, Participants = g.OrderBy(x => x.Name).Select(x => x.Name).ToList(), Count = g.Count() })
            .OrderBy(g => g.Family)
            .ToList();

        var total = grouped.Count;
        var pageItems = pageLimit > 0 ? grouped.Skip((page - 1) * pageLimit).Take(pageLimit) : grouped;

        var columns = new[]
        {
            new ColumnDef("family", "Família"),
            new ColumnDef("participants", "Participantes"),
            new ColumnDef("count", "Qtde")
        };

        var data = pageItems.Select(x => (IDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["family"] = x.Family,
            ["participants"] = string.Join(", ", x.Participants),
            ["count"] = x.Count
        }).ToList();

        var header = new ReportHeader(ctx.ReportId, string.IsNullOrWhiteSpace(ctx.Title) ? DefaultTitle : ctx.Title, DateTime.UtcNow, ctx.RetreatId, ctx.RetreatName);

        return new ReportPayload(header, columns, data,
            new Dictionary<string, object?> { ["totalFamilies"] = grouped.Count, ["totalParticipants"] = grouped.Sum(x => x.Count) },
            total, page, pageLimit);
    }
}
