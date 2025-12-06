using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.API.Auth;
using SAMGestor.Application.Dtos.Dashboards;
using SAMGestor.Application.Features.Dashboards.Families;
using SAMGestor.Application.Features.Dashboards.Overview;
using SAMGestor.Application.Features.Dashboards.Payments;
using SAMGestor.Application.Features.Dashboards.Service;
using Swashbuckle.AspNetCore.Annotations;

namespace SAMGestor.API.Controllers.DashboardsController;

[ApiController]
[Route("api/dashboards")]
[SwaggerTag("Operações relacionadas às telas de painel de controle. (Admin,Gestor,Consultor)")]
[Authorize(Policy = Policies.ReadOnly)] 
public class DashboardsController : ControllerBase
{
    private readonly IMediator _mediator;
    public DashboardsController(IMediator mediator) => _mediator = mediator;

    /// <summary> Resumo geral de um retiro.</summary>
    [HttpGet("overview")]
    public async Task<ActionResult<DashboardOverviewDto>> Overview(
        [FromQuery] Guid retreatId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
    {
        var dto = await _mediator.Send(new GetOverviewQuery(retreatId, from, to), ct);
        return Ok(dto);
    }

    /// <summary> Lista de famílias com métricas.</summary>
    [HttpGet("families")]
    public async Task<ActionResult<FamiliesListDto>> Families(
        [FromQuery] Guid retreatId,
        [FromQuery] int top = 10,
        CancellationToken ct = default)
    {
        var dto = await _mediator.Send(new GetFamiliesQuery(retreatId, top), ct);
        return Ok(dto);
    }

    /// <summary> Séries de pagamentos confirmados e pendentes.</summary>
    [HttpGet("payments/timeseries")]
    public async Task<ActionResult<PaymentPointDto[]>> PaymentsSeries(
        [FromQuery] Guid retreatId,
        [FromQuery] TimeInterval interval = TimeInterval.Daily,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        CancellationToken ct = default)
    {
        var dto = await _mediator.Send(new GetPaymentsSeriesQuery(retreatId, interval, from, to), ct);
        return Ok(dto);
    }

    /// <summary> Resumo de métricas do serviço.</summary>
    // NOVO
    [HttpGet("service/overview")]
    public async Task<ActionResult<OverviewServiceDto>> ServiceOverview(
        [FromQuery] Guid retreatId,
        CancellationToken ct = default)
    {
        var dto = await _mediator.Send(new GetServiceOverviewQuery(retreatId), ct);
        return Ok(dto);
    }
}
