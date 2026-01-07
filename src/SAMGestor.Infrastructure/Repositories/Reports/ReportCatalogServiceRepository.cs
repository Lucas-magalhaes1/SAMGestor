using Microsoft.EntityFrameworkCore;
using SAMGestor.Application.Common.Pagination;
using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Interfaces.Reports;
using SAMGestor.Domain.Entities;
using SAMGestor.Infrastructure.Persistence;

namespace SAMGestor.Infrastructure.Repositories.Reports;

public sealed class ReportCatalogServiceRepository : IReportCatalog
{
    private readonly SAMContext _db;

    public ReportCatalogServiceRepository(SAMContext db) => _db = db;

    public async Task<PagedResult<ReportListItemDto>> ListAsync(int skip, int take, CancellationToken ct)
    {
        var query = _db.Reports.AsNoTracking().OrderByDescending(r => r.DateCreation);

        var totalCount = await query.CountAsync(ct);
        
        var items = await query
            .ApplyPagination(skip, take)
            .Select(r => new ReportListItemDto(
                r.Id.ToString(),
                r.Title,
                r.DateCreation
            ))
            .ToListAsync(ct);

        return new PagedResult<ReportListItemDto>(items, totalCount, skip, take);
    }

    public async Task<string> CreateAsync(CreateReportRequest request, CancellationToken ct)
    {
        var entity = new Report(
            title: request.Title,
            templateKey: request.TemplateKey,
            retreatId: request.RetreatId,
            createdByUserId: null,
            defaultParamsJson: request.DefaultParamsJson
        );

        _db.Reports.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.Id.ToString();
    }

    public async Task<ReportListItemDto?> UpdateAsync(string id, UpdateReportRequest request, CancellationToken ct)
    {
        if (!Guid.TryParse(id, out var gid)) return null;

        var entity = await _db.Reports.FirstOrDefaultAsync(r => r.Id == gid, ct);
        if (entity is null) return null;

        entity.Rename(request.Title);
        if (!string.IsNullOrWhiteSpace(request.TemplateKey))
            entity.ChangeTemplate(request.TemplateKey);
        if (request.DefaultParamsJson is not null)
            entity.SetDefaultParams(request.DefaultParamsJson);

        await _db.SaveChangesAsync(ct);

        return new ReportListItemDto(entity.Id.ToString(), entity.Title, entity.DateCreation);
    }

    public async Task<(bool ok, string id)> DeleteAsync(string id, CancellationToken ct)
    {
        if (!Guid.TryParse(id, out var gid)) return (false, id);

        var entity = await _db.Reports.FirstOrDefaultAsync(r => r.Id == gid, ct);
        if (entity is null) return (false, id);

        _db.Reports.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return (true, id);
    }
    
    public async Task<PagedResult<ReportListItemDto>> ListByRetreatAsync(Guid retreatId, int skip, int take, CancellationToken ct)
    {
        var query = _db.Reports
            .AsNoTracking()
            .Where(r => r.RetreatId == retreatId)
            .OrderByDescending(r => r.DateCreation);

        var totalCount = await query.CountAsync(ct);
        
        var items = await query
            .ApplyPagination(skip, take)
            .Select(r => new ReportListItemDto(r.Id.ToString(), r.Title, r.DateCreation))
            .ToListAsync(ct);

        return new PagedResult<ReportListItemDto>(items, totalCount, skip, take);
    }
}
