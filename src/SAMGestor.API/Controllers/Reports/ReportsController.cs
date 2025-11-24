using MediatR;
using Microsoft.AspNetCore.Mvc;
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
public sealed class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ReportsController(IMediator mediator) => _mediator = mediator;
    

    /// <summary> Lista os relatórios disponíveis. </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ReportListItemDto>>> List([FromQuery] int page = 1, [FromQuery] int limit = 10, CancellationToken ct = default)
        => Ok(await _mediator.Send(new ListReportsQuery(page, limit), ct));

    /// <summary> Cria um novo relatório. </summary>
    [HttpPost]
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

    /// <summary> Detalhe de um relatório. </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ReportPayload>> Detail(string id, [FromQuery] int page = 1, [FromQuery] int pageLimit = 0, CancellationToken ct = default)
    {
        var payload = await _mediator.Send(new GetReportDetailQuery(id, page, pageLimit), ct);
        if (payload is null) return NotFound(new { error = "Relatório não encontrado" });
        return Ok(payload);
    }

    /// <summary> Atualiza dados básicos do relatório. </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ReportListItemDto>> Update(string id, [FromBody] UpdateReportCommand body, CancellationToken ct)
    {
        var updated = await _mediator.Send(body with { Id = id }, ct);
        if (updated is null) return NotFound(new { error = "Relatório não encontrado" });
        return Ok(updated);
    }

    /// <summary> Exclui um relatório. </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<object>> Delete(string id, CancellationToken ct)
    {
        var ok = await _mediator.Send(new DeleteReportCommand(id), ct);
        if (!ok) return NotFound(new { error = "Relatório não encontrado" });
        return Ok(new { message = "Relatório excluído com sucesso", id });
    }
    
    /// <summary> Lista os templates de relatório disponíveis. </summary>
    [HttpGet("templates")]
    public async Task<ActionResult<IReadOnlyList<ReportTemplateSchemaDto>>> Templates(CancellationToken ct)
        => Ok(await _mediator.Send(new GetTemplatesSchemasQuery(), ct));    
    
    /// <summary> Lista os relatórios de um retiro. </summary>
    [HttpGet("retreats/{retreatId:guid}")]
    public async Task<ActionResult<PaginatedResponse<ReportListItemDto>>> ListByRetreat(
        Guid retreatId, [FromQuery] int page = 1, [FromQuery] int limit = 10, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ListReportsByRetreatQuery(retreatId, page, limit), ct);
        return Ok(result);
    }
    
    public enum ReportExportFormat
    {
        csv,
        pdf
    }
    
    /// <summary> Exporta um relatório. </summary>
    [HttpGet("{id}/export")]
    public async Task<IActionResult> Export(
        string id,
        [FromQuery] ReportExportFormat format = ReportExportFormat.csv,
        [FromQuery] string? fileName = null,
        CancellationToken ct = default)
    {
        var payload = await _mediator.Send(new GetReportDetailQuery(id, Page: 1, PageLimit: 0), ct);
        if (payload is null) return NotFound(new { error = "Relatório não encontrado" });

        var exporter = HttpContext.RequestServices.GetRequiredService<IReportExporter>();
        var (contentType, file, bytes) = await exporter.ExportAsync(payload, format.ToString(), fileName, ct);
        return File(bytes, contentType, file);
    }
    
}


