using MediatR;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.Application.Features.Dev.Seed;

namespace SAMGestor.API.Controllers.SeedController;

[ApiController]
[Route("api/dev")]
public class DevController : ControllerBase
{
    private readonly ISender _sender;

    public DevController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Cria dados de teste: 2 retiros com inscritos fake.
    /// Seed 1: 300 inscritos NotSelected (pra contemplação).
    /// Seed 2: 200 inscritos Confirmed/PaymentConfirmed (pra famílias/barracas).
    /// </summary>
    [HttpPost("seed")]
    public async Task<IActionResult> SeedTestData(CancellationToken ct)
    {
        var result = await _sender.Send(new SeedTestDataCommand(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Remove todos os retiros [SEED] e seus inscritos.
    /// </summary>
    [HttpDelete("seed")]
    public async Task<IActionResult> ClearSeeds(CancellationToken ct)
    {
        var result = await _sender.Send(new ClearSeedDataCommand(), ct);
        return Ok(result);
    }
}