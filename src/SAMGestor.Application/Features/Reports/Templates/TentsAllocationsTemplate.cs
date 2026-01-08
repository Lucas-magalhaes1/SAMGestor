using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Reports.Templates;

public sealed class TentsAllocationsTemplate : IDescribedReportTemplate
{
    public const string TemplateKeyConst = "tents.allocations";
    public string Key => TemplateKeyConst;
    public string DefaultTitle => "Barracas e Rahamistas (Pagos)";

    private readonly IReportingReadDb _readDb;
    public TentsAllocationsTemplate(IReportingReadDb readDb) => _readDb = readDb;
    
    public ReportTemplateSchemaDto Describe() => new(
        Key,
        DefaultTitle,
        new[]
        {
            new ColumnDef("tent", "Barraca"),
            new ColumnDef("participant", "Rahamista"),
            new ColumnDef("gender", "Sexo"),
            new ColumnDef("family", "Família"),
            new ColumnDef("shirt", "Camiseta")
        },
        new[] { "totalTents", "totalAssigned" },
        SupportsPaging: true,
        DefaultPageLimit: 50
    );

    public async Task<ReportPayload> GetDataAsync(ReportContext ctx, int page, int pageLimit, CancellationToken ct)
    {
        // tents por retiro (se informado)
        var tentsQuery = _readDb.AsNoTracking().Tents.AsQueryable();
        if (ctx.RetreatId.HasValue)
            tentsQuery = tentsQuery.Where(t => t.RetreatId == ctx.RetreatId.Value);
        var tents = await _readDb.ToListAsync(
            tentsQuery.Select(t => new
            {
                t.Id,
                Number = t.Number.Value, // se for value object
                t.RetreatId
            }),
            ct
        );
        var tentById = tents.ToDictionary(t => t.Id, t => t.Number);

        // confirmados por retiro
        var regsBase = _readDb.AsNoTracking().Registrations
            .Where(r => r.Status == RegistrationStatus.Confirmed);
        if (ctx.RetreatId.HasValue)
            regsBase = regsBase.Where(r => r.RetreatId == ctx.RetreatId.Value);

        // pagos set
        var paidIds = await _readDb.ToListAsync(
            _readDb.AsNoTracking().Payments
                .Where(p => p.Status == PaymentStatus.Paid)
                .Select(p => p.RegistrationId),
            ct
        );
        var paidSet = paidIds.ToHashSet();

        // alocações do retiro
        var allocations = await _readDb.ToListAsync(
            _readDb.AsNoTracking().TentAssignments
                .Select(a => new { a.TentId, a.RegistrationId }),
            ct
        );

        // famílias map
        var fm = await _readDb.ToListAsync(
            _readDb.AsNoTracking().FamilyMembers
                .Select(f => new { f.FamilyId, f.RegistrationId }),
            ct
        );
        var families = await _readDb.ToListAsync(
            _readDb.AsNoTracking().Families.Select(f => new { f.Id, Name = f.Name.Value }),
            ct
        );
        var famById = families.ToDictionary(x => x.Id, x => x.Name);

        // registros confirmados + pagos (para montar linhas)
        var regs = await _readDb.ToListAsync(
            regsBase
                .Where(r => paidSet.Contains(r.Id))
                .Select(r => new
                {
                    r.Id,
                    Name = r.Name.Value,
                    r.Gender,
                    Shirt = r.ShirtSize.HasValue ? r.ShirtSize.Value.ToString() : "",
                }),
            ct
        );

        // junta: tent + participant + family
        var rows = allocations
            .Where(a => tentById.ContainsKey(a.TentId))
            .Join(regs, a => a.RegistrationId, r => r.Id, (a, r) =>
            {
                var famId = fm.FirstOrDefault(x => x.RegistrationId == r.Id)?.FamilyId;
                var family = famId.HasValue && famById.TryGetValue(famId.Value, out var n) ? n : null;
                return new
                {
                    TentLabel = tentById[a.TentId],
                    Participant = r.Name,
                    Gender = r.Gender.ToString(),
                    Family = family,
                    Shirt = r.Shirt
                };
            })
            .OrderBy(x => x.TentLabel)
            .ThenBy(x => x.Participant)
            .ToList();

        // paginação opcional
        var total = rows.Count;
        var pageItems = pageLimit > 0 ? rows.Skip((page - 1) * pageLimit).Take(pageLimit) : rows;

        var columns = new[]
        {
            new ColumnDef("tent",       "Barraca"),
            new ColumnDef("participant","Rahamista"),
            new ColumnDef("gender",     "Sexo"),
            new ColumnDef("family",     "Família"),
            new ColumnDef("shirt",      "Camiseta")
        };

        var data = pageItems.Select(x =>
            (IDictionary<string, object?>) new Dictionary<string, object?>
            {
                ["tent"]        = x.TentLabel,
                ["participant"] = x.Participant,
                ["gender"]      = x.Gender,
                ["family"]      = x.Family,
                ["shirt"]       = x.Shirt
            }).ToList();

        var header = new ReportHeader(
            ctx.ReportId,
            string.IsNullOrWhiteSpace(ctx.Title) ? DefaultTitle : ctx.Title,
            DateTime.UtcNow, 
            ctx.RetreatId,
            ctx.RetreatName
        );

        return new ReportPayload(
            report: header,
            columns: columns,
            data: data,
            summary: new Dictionary<string, object?>
            {
                ["totalTents"]    = tents.Count,
                ["totalAssigned"] = rows.Count
            },
            total: total,
            page: page,
            pageLimit: pageLimit
        );
    }
}
