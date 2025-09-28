using MediatR;
using Microsoft.AspNetCore.Mvc;
using SAMGestor.Application.Features.Service.Registrations.Create;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.API.Controllers;

[ApiController]
[Route("api/retreats/{retreatId:guid}/service/registrations")]
public class ServiceRegistrationsController(IMediator mediator) : ControllerBase
{
    public sealed record CreateServiceRegistrationRequest(
        string   Name,
        string   Cpf,
        string   Email,
        string   Phone,
        DateOnly BirthDate,
        Gender   Gender,
        string   City,
        string   Region,
        Guid?    PreferredSpaceId
    );

    [HttpPost]
    public async Task<ActionResult<CreateServiceRegistrationResponse>> Create(
        Guid retreatId,
        [FromBody] CreateServiceRegistrationRequest req,
        CancellationToken ct)
    {
        var cmd = new CreateServiceRegistrationCommand(
            RetreatId: retreatId,
            Name:  new FullName(req.Name),
            Cpf:   new CPF(req.Cpf),
            Email: new EmailAddress(req.Email),
            Phone: req.Phone,
            BirthDate: req.BirthDate,
            Gender: req.Gender,
            City: req.City,
            Region: req.Region,
            PreferredSpaceId: req.PreferredSpaceId
        );

        var result = await mediator.Send(cmd, ct);
        return CreatedAtAction(nameof(GetByIdPlaceholder), new { retreatId, id = result.ServiceRegistrationId }, result);
    }
    
    [HttpGet("{id:guid}")]
    public IActionResult GetByIdPlaceholder(Guid retreatId, Guid id) => NoContent();
}