using Microsoft.EntityFrameworkCore;
using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;
using SAMGestor.Infrastructure.Persistence;

namespace SAMGestor.Infrastructure.Repositories.Reports;

public sealed class ReportEngineRepository : IReportEngine
{
    private readonly SAMContext _db;
    private readonly IReadOnlyDictionary<string, IReportTemplate> _templates;

    public ReportEngineRepository(SAMContext db, IEnumerable<IReportTemplate> templates)
    {
        _db = db;
        _templates = templates.ToDictionary(t => t.Key, t => t, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<ReportContext> BuildContextAsync(string reportId, CancellationToken ct)
    {
        if (!Guid.TryParse(reportId, out var gid))
            throw new KeyNotFoundException("Invalid report id.");

        var r = await _db.Reports.AsNoTracking().FirstOrDefaultAsync(x => x.Id == gid, ct);
        if (r is null) throw new KeyNotFoundException("Relatório não encontrado");

        // ← BUSCAR NOME DO RETIRO
        string? retreatName = null;
        if (r.RetreatId.HasValue)
        {
            retreatName = await _db.Retreats
                .AsNoTracking() 
                .Where(ret => ret.Id == r.RetreatId.Value)
                .Select(ret => ret.Name)
                .FirstOrDefaultAsync(ct);
        }

        return new ReportContext(
            ReportId: r.Id.ToString(),
            Title: r.Title,
            TemplateKey: r.TemplateKey,
            RetreatId: r.RetreatId,
            DefaultParamsJson: r.DefaultParamsJson,
            RetreatName: retreatName  
        );
    }

    public async Task<ReportPayload> GetPayloadAsync(ReportContext ctx, int skip, int take, CancellationToken ct)
    {
        if (!_templates.TryGetValue(ctx.TemplateKey, out var template))
            throw new KeyNotFoundException($"Template '{ctx.TemplateKey}' não registrado.");

        var payload = await template.GetDataAsync(ctx, skip, take, ct);

        if (string.IsNullOrWhiteSpace(payload.report.Title))
        {
            var header = payload.report with { Title = template.DefaultTitle };
            payload = payload with { report = header };
        }

        return payload;
    }
}
