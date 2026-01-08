using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Reports.Templates;

public sealed class ShirtsBySizeTemplate : IDescribedReportTemplate
{
    public const string TemplateKeyConst = "shirts.by-size";
    public string Key => TemplateKeyConst;
    public string DefaultTitle => "Camisetas por Tamanho";

    private readonly IReportingReadDb _readDb;

    public ShirtsBySizeTemplate(IReportingReadDb readDb) => _readDb = readDb;

    private static string MapSize(ShirtSize s) => s switch
    {
     
        ShirtSize.P  => "P",
        ShirtSize.M  => "M",
        ShirtSize.G  => "G",
        ShirtSize.GG => "GG",
        ShirtSize.GG1 => "GG1",
        ShirtSize.GG2 => "GG2",
        ShirtSize.GG3 => "GG3",
        ShirtSize.GG4 => "GG4",
        _ => "—" 
    };
    
    public ReportTemplateSchemaDto Describe() => new(
        Key,
        DefaultTitle,
        new[] { new ColumnDef("size","Tamanho"), new ColumnDef("count","Qtde") },
        new[] { "totalParticipants" },
        SupportsPaging: false,
        DefaultPageLimit: 0
    );

    public async Task<ReportPayload> GetDataAsync(
        ReportContext ctx, int page, int pageLimit, CancellationToken ct)
    {
        
        var regs = _readDb.AsNoTracking().Registrations
            .Where(r => r.Status == RegistrationStatus.Confirmed);

        if (ctx.RetreatId.HasValue)
            regs = regs.Where(r => r.RetreatId == ctx.RetreatId.Value);

        var paidIds = await _readDb.ToListAsync(
            _readDb.AsNoTracking().Payments
                .Where(p => p.Status == PaymentStatus.Paid)
                .Select(p => p.RegistrationId),
            ct
        );
        var paidSet = paidIds.ToHashSet();
        
        var validSizes = await _readDb.ToListAsync(
            regs
                .Where(r => r.ShirtSize.HasValue && (int)r.ShirtSize.Value != 0 && paidSet.Contains(r.Id))
                .Select(r => r.ShirtSize!.Value),
            ct
        );

       
        var grouped = validSizes
            .GroupBy(s => s)
            .Select(g => new { Size = g.Key, Count = g.Count() })
            .OrderBy(x => x.Size)
            .ToList();

        var columns = new[]
        {
            new ColumnDef("size",  "Tamanho"),
            new ColumnDef("count", "Qtde")
        };

        var data = grouped
            .Select(x => (IDictionary<string, object?>) new Dictionary<string, object?>
            {
                ["size"] = x.Size.ToString(),
                ["count"] = x.Count
            })
            .ToList();

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
                ["totalParticipants"] = grouped.Sum(x => x.Count)
            },
            total: data.Count,
            page: 1,
            pageLimit: 0
        );
    }
}
