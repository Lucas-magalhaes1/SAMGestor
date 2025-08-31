using Microsoft.AspNetCore.Mvc;
using SAMGestor.Contracts;
using SAMGestor.Notification.Application.Abstractions;

namespace SAMGestor.Notification.API.Controllers;

[ApiController]
[Route("admin/dev")]
public class AdminController(IEventPublisher publisher) : ControllerBase
{
    /// <summary>
    /// Simula o Core publicando selection.participant.selected.v1.
    /// Útil para testar o fluxo completo por eventos.
    /// </summary>
    
    [HttpPost("selection")]
    public async Task<IActionResult> SimulateSelection([FromBody] SelectionParticipantSelectedV1 dto, CancellationToken ct)
    {
        await publisher.PublishAsync(
            type: EventTypes.SelectionParticipantSelectedV1,
            source: "sam.core.dev",
            data: dto,
            traceId: null,
            ct: ct
        );

        return Accepted(new { status = "published", dto.RegistrationId, dto.Email });
    }
}