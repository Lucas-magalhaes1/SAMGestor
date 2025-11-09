// SAMGestor.API/Controllers/DashboardsController.cs
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.Application.Dtos.Dashboards;
using SAMGestor.Application.Features.Dashboards.Families;
using SAMGestor.Application.Features.Dashboards.Overview;
using SAMGestor.Application.Features.Dashboards.Payments;
using SAMGestor.Application.Features.Dashboards.Service;

namespace SAMGestor.API.Controllers;

[ApiController]
[Route("api/dashboards")]
public class DashboardsController : ControllerBase
{
    private readonly IMediator _mediator;
    public DashboardsController(IMediator mediator) => _mediator = mediator;

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

    [HttpGet("families")]
    public async Task<ActionResult<FamiliesListDto>> Families(
        [FromQuery] Guid retreatId,
        [FromQuery] int top = 10,
        CancellationToken ct = default)
    {
        var dto = await _mediator.Send(new GetFamiliesQuery(retreatId, top), ct);
        return Ok(dto);
    }

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
