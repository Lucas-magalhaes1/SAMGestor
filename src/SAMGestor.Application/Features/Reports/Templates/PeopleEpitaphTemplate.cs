using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Reports.Templates;

public sealed class PeopleEpitaphTemplate : IDescribedReportTemplate
{
    public const string TemplateKeyConst = "people.epitaph";
    public string Key => TemplateKeyConst;
    public string DefaultTitle => "Lápides (Foto/Link/Data Nasc.)";

    private readonly IReportingReadDb _readDb;
    public PeopleEpitaphTemplate(IReportingReadDb readDb) => _readDb = readDb;

    public ReportTemplateSchemaDto Describe() => new(
        Key,
        DefaultTitle,
        new[]
        {
            new ColumnDef("name", "Nome"),
            new ColumnDef("birthDate", "Nascimento"),
            new ColumnDef("photoUrl", "Foto/Link"),
            new ColumnDef("city", "Cidade"),
            new ColumnDef("family", "Família")
        },
        new[] { "total" },
        SupportsPaging: true,
        DefaultPageLimit: 50
    );

    public async Task<ReportPayload> GetDataAsync(ReportContext ctx, int page, int pageLimit, CancellationToken ct)
    {
        var regsBase = _readDb.AsNoTracking().Registrations.Where(r => r.Status == RegistrationStatus.Confirmed);
        if (ctx.RetreatId.HasValue) regsBase = regsBase.Where(r => r.RetreatId == ctx.RetreatId.Value);

        var paidIds = await _readDb.ToListAsync(
            _readDb.AsNoTracking().Payments.Where(p => p.Status == PaymentStatus.Paid).Select(p => p.RegistrationId), ct);
        var paidSet = paidIds.ToHashSet();

        var fm = await _readDb.ToListAsync(_readDb.AsNoTracking().FamilyMembers.Select(f => new { f.FamilyId, f.RegistrationId }), ct);
        var families = await _readDb.ToListAsync(_readDb.AsNoTracking().Families.Select(f => new { f.Id, Name = f.Name.Value }), ct);
        var famById = families.ToDictionary(x => x.Id, x => x.Name);

        var regs = await _readDb.ToListAsync(
            regsBase.Where(r => paidSet.Contains(r.Id)).Select(r => new
            {
                r.Id,
                Name = r.Name.Value,
                r.City,
                r.BirthDate,
                Photo = r.PhotoUrl != null ? r.PhotoUrl.Value : null,
                Doc = r.IdDocumentUrl != null ? r.IdDocumentUrl.Value : null
            }), ct);

        var rows = regs.Select(r =>
        {
            var famId = fm.FirstOrDefault(x => x.RegistrationId == r.Id)?.FamilyId;
            var family = famId.HasValue && famById.TryGetValue(famId.Value, out var n) ? n : null;
            var url = r.Photo ?? r.Doc ?? "";
            return new
            {
                r.Name,
                r.City,
                BirthDate = r.BirthDate.ToDateTime(TimeOnly.MinValue).ToString("yyyy-MM-dd"),
                PhotoUrl = url,
                Family = family
            };
        })
        .OrderBy(x => x.Family ?? "ZZZ")
        .ThenBy(x => x.Name)
        .ToList();

        var total = rows.Count;
        var pageItems = pageLimit > 0 ? rows.Skip((page - 1) * pageLimit).Take(pageLimit) : rows;

        var columns = new[]
        {
            new ColumnDef("name", "Nome"),
            new ColumnDef("birthDate", "Nascimento"),
            new ColumnDef("photoUrl", "Foto/Link"),
            new ColumnDef("city", "Cidade"),
            new ColumnDef("family", "Família")
        };

        var data = pageItems.Select(x => (IDictionary<string, object?>)new Dictionary<string, object?>
        {
            ["name"] = x.Name,
            ["birthDate"] = x.BirthDate,
            ["photoUrl"] = x.PhotoUrl,
            ["city"] = x.City,
            ["family"] = x.Family
        }).ToList();

        var header = new ReportHeader(ctx.ReportId, string.IsNullOrWhiteSpace(ctx.Title) ? DefaultTitle : ctx.Title, DateTime.UtcNow);

        return new ReportPayload(header, columns, data,
            new Dictionary<string, object?> { ["total"] = total }, total, page, pageLimit);
    }
}
