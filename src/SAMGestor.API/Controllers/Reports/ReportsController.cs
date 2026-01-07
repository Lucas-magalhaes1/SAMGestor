using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.API.Auth;
using SAMGestor.Application.Common.Pagination;
using SAMGestor.Application.Dtos.Reports;
using SAMGestor.Application.Features.Reports.Create;
using SAMGestor.Application.Features.Reports.Delete;
using SAMGestor.Application.Features.Reports.GetDetail;
using SAMGestor.Application.Features.Reports.List;
using SAMGestor.Application.Features.Reports.ListByRetreat;
using SAMGestor.Application.Features.Reports.TemplatesList;
using SAMGestor.Application.Features.Reports.Update;
using SAMGestor.Application.Interfaces.Reports;
using Swashbuckle.AspNetCore.Annotations;

namespace SAMGestor.API.Controllers.Reports;

[ApiController]
[Route("api/reports")]
[SwaggerTag("Operações relacionadas às gerações de relatórios.")]
[Authorize(Policy = Policies.ReadOnly)]
public sealed class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ReportsController(IMediator mediator) => _mediator = mediator;

    /// <summary> Lista os relatórios disponíveis. (Admin,Gestor,Consultor)</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ReportListItemDto>>> List(
        [FromQuery] int skip = 0, 
        [FromQuery] int take = 10, 
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new ListReportsQuery(skip, take), ct));

    /// <summary> Cria um novo relatório. (Admin, Gestor)</summary>
    [HttpPost]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    public async Task<ActionResult<object>> Create([FromBody] CreateReportCommand cmd, CancellationToken ct)
    {
        var created = await _mediator.Send(cmd, ct);
        return CreatedAtAction(nameof(Detail), new { id = created.Id }, new
        {
            id = created.Id,
            title = created.Title,
            dateCreation = created.DateCreation
        });
    }

    /// <summary> Detalhe de um relatório.(Admin,Gestor,Consultor) </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ReportPayload>> Detail(
        string id, 
        [FromQuery] int skip = 0, 
        [FromQuery] int take = 0, 
        CancellationToken ct = default)
    {
        var payload = await _mediator.Send(new GetReportDetailQuery(id, skip, take), ct);
        if (payload is null) return NotFound(new { error = "Relatório não encontrado" });
        return Ok(payload);
    }

    /// <summary> Atualiza dados básicos do relatório. (Admin, Gestor)</summary>
    [HttpPut("{id}")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    public async Task<ActionResult<ReportListItemDto>> Update(string id, [FromBody] UpdateReportCommand body, CancellationToken ct)
    {
        var updated = await _mediator.Send(body with { Id = id }, ct);
        if (updated is null) return NotFound(new { error = "Relatório não encontrado" });
        return Ok(updated);
    }

    /// <summary> Exclui um relatório. (Admin, Gestor)</summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.ManagerOrAbove)]
    public async Task<ActionResult<object>> Delete(string id, CancellationToken ct)
    {
        var ok = await _mediator.Send(new DeleteReportCommand(id), ct);
        if (!ok) return NotFound(new { error = "Relatório não encontrado" });
        return Ok(new { message = "Relatório excluído com sucesso", id });
    }
    
    /// <summary> Lista os templates de relatório disponíveis. (Admin,Gestor,Consultor)</summary>
    [HttpGet("templates")]
    public async Task<ActionResult<IReadOnlyList<ReportTemplateSchemaDto>>> Templates(CancellationToken ct)
        => Ok(await _mediator.Send(new GetTemplatesSchemasQuery(), ct));    
    
    /// <summary> Lista os relatórios de um retiro. (Admin,Gestor,Consultor)</summary>
    [HttpGet("retreats/{retreatId:guid}")]
    public async Task<ActionResult<PagedResult<ReportListItemDto>>> ListByRetreat(
        Guid retreatId, 
        [FromQuery] int skip = 0, 
        [FromQuery] int take = 10, 
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListReportsByRetreatQuery(retreatId, skip, take), ct);
        return Ok(result);
    }
    
    public enum ReportExportFormat
    {
        csv,
        pdf
    }
    
    /// <summary> Exporta um relatório. (Admin,Gestor,Consultor)</summary>
    [HttpGet("{id}/export")]
    public async Task<IActionResult> Export(
        string id,
        [FromQuery] ReportExportFormat format = ReportExportFormat.csv,
        [FromQuery] string? fileName = null,
        CancellationToken ct = default)
    {
        // Export pega tudo (take=0)
        var payload = await _mediator.Send(new GetReportDetailQuery(id, Skip: 0, Take: 0), ct);
        if (payload is null) return NotFound(new { error = "Relatório não encontrado" });

        var exporter = HttpContext.RequestServices.GetRequiredService<IReportExporter>();
        var (contentType, file, bytes) = await exporter.ExportAsync(payload, format.ToString(), fileName, ct);
        return File(bytes, contentType, file);
    }
}
