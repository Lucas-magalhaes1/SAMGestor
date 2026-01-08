using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Reports.Templates;

public sealed class ContemplatedGeneralTemplate : IDescribedReportTemplate
{
    public const string TemplateKeyConst = "contemplated.general";
    public string Key => TemplateKeyConst;
    public string DefaultTitle => "Contemplados (Pagamento)";

    private readonly IReportingReadDb _readDb;
    public ContemplatedGeneralTemplate(IReportingReadDb readDb) => _readDb = readDb;

    public ReportTemplateSchemaDto Describe() => new(
        Key,
        DefaultTitle,
        new[]
        {
            new ColumnDef("name", "Nome"),
            new ColumnDef("family", "Família"),
            new ColumnDef("city", "Cidade"),
            new ColumnDef("shirt", "Camiseta"),
            new ColumnDef("payment", "Pagamento")
        },
        new[] { "totalConfirmed", "totalPaid", "totalPending" },
        SupportsPaging: true,
        DefaultPageLimit: 20
    );

    public async Task<ReportPayload> GetDataAsync(ReportContext ctx, int page, int pageLimit, CancellationToken ct)
    {
        var regs = _readDb.AsNoTracking().Registrations.Where(r => r.Status == RegistrationStatus.Confirmed);
        if (ctx.RetreatId.HasValue) regs = regs.Where(r => r.RetreatId == ctx.RetreatId.Value);

        var fm = await _readDb.ToListAsync(_readDb.AsNoTracking().FamilyMembers.Select(f => new { f.FamilyId, f.RegistrationId }), ct);
        var families = await _readDb.ToListAsync(_readDb.AsNoTracking().Families.Select(f => new { f.Id, Name = f.Name.Value }), ct);
        var famById = families.ToDictionary(x => x.Id, x => x.Name);

        var regsFlat = await _readDb.ToListAsync(regs.Select(r => new
        {
            r.Id,
            Name = r.Name.Value,
            r.City,
            Shirt = r.ShirtSize.HasValue ? r.ShirtSize.Value.ToString() : ""
        }), ct);

        var paid = await _readDb.ToListAsync(
            _readDb.AsNoTracking().Payments.Where(p => p.Status == PaymentStatus.Paid).Select(p => new { p.RegistrationId }), ct);
        var paidSet = paid.Select(p => p.RegistrationId).ToHashSet();

        var rowsAll = regsFlat.Select(r =>
        {
            var famId = fm.FirstOrDefault(x => x.RegistrationId == r.Id)?.FamilyId;
            var family = famId.HasValue && famById.TryGetValue(famId.Value, out var n) ? n : null;
            var isPaid = paidSet.Contains(r.Id);
            return new
            {
                r.Name,
                Family = family,
                r.City,
                r.Shirt,
                Payment = isPaid ? "ok" : "pendente",
                Paid = isPaid
            };
        }).ToList();

        var total = rowsAll.Count;
        var totalPaid = rowsAll.Count(x => x.Paid);
        var totalPending = total - totalPaid;

        var rows = pageLimit > 0 ? rowsAll.Skip((page - 1) * pageLimit).Take(pageLimit) : rowsAll;

        var columns = new[]
        {
            new ColumnDef("name", "Nome"),
            new ColumnDef("family", "Família"),
            new ColumnDef("city", "Cidade"),
            new ColumnDef("shirt", "Camiseta"),
            new ColumnDef("payment", "Pagamento")
        };

        var data = rows.Select(x => (IDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["name"] = x.Name,
            ["family"] = x.Family,
            ["city"] = x.City,
            ["shirt"] = x.Shirt,
            ["payment"] = x.Payment
        }).ToList();

        var header = new ReportHeader(ctx.ReportId, string.IsNullOrWhiteSpace(ctx.Title) ? DefaultTitle : ctx.Title, DateTime.UtcNow, ctx.RetreatId, ctx.RetreatName);

        return new ReportPayload(header, columns, data,
            new Dictionary<string, object?> { ["totalConfirmed"] = total, ["totalPaid"] = totalPaid, ["totalPending"] = totalPending },
            total, page, pageLimit);
    }
}
