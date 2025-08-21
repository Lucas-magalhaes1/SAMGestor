using Microsoft.AspNetCore.Mvc;
using SAMGestor.Contracts;
using SAMGestor.Notification.Application.Orchestrators;

namespace SAMGestor.Notification.API.Controllers;

[ApiController]
[Route("admin")]
public class AdminController : ControllerBase
{
    private readonly NotificationOrchestrator _orchestrator;

    public AdminController(NotificationOrchestrator orchestrator) => _orchestrator = orchestrator;
    
    [HttpPost("notifications/test/participant-selected")]
    public async Task<IActionResult> SimulateSelected([FromBody] SelectionParticipantSelectedV1 dto, CancellationToken ct)
    {
        await _orchestrator.OnParticipantSelectedAsync(dto, ct);
        return Ok(new { status = "queued", dto.RegistrationId, dto.ParticipantId, dto.Email });
    }
}