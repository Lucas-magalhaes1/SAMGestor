using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SAMGestor.API.Controllers;
using SAMGestor.Application.Features.Registrations.Create;
using SAMGestor.Application.Features.Registrations.GetAll;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.API.Controllers;

public class RegistrationsControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly RegistrationsController _controller;

    public RegistrationsControllerTests()
    {
        _controller = new RegistrationsController(_mediator.Object);
    }

    [Fact]
    public async Task Create_Returns_CreatedAtRoute_With_RegistrationId()
    {
        var regId = Guid.NewGuid();
        _mediator
            .Setup(m => m.Send(It.IsAny<CreateRegistrationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateRegistrationResponse(regId));

        var cmd = new CreateRegistrationCommand(
            RetreatId: Guid.NewGuid(),
            Name: new FullName("Fulano Teste"),
            Cpf: new CPF("12345678901"),         
            Email: new EmailAddress("x@x.com"),
            Phone: "11999999999",
            BirthDate: new DateOnly(2000, 1, 1),
            Gender: Gender.Male,
            City: "SP",
            ParticipationCategory: ParticipationCategory.Guest,
            Region: "Oeste"
        );
        
        var result = await _controller.Create(cmd);
        
        var created = result as CreatedAtRouteResult;
        created.Should().NotBeNull();
        created!.RouteName.Should().Be(nameof(RegistrationsController.GetById));
        created.RouteValues!["id"].Should().Be(regId);
        created.Value.Should().BeOfType<CreateRegistrationResponse>()
               .Which.RegistrationId.Should().Be(regId);
    }

    [Fact]
    public async Task List_Returns_Ok_With_Query_Response()
    {
        
        var retreatId = Guid.NewGuid();
        
        var emptyItems = new List<RegistrationDto>();
        var response = new GetAllRegistrationsResponse(emptyItems, 0, 0, 20);

        _mediator
            .Setup(m => m.Send(It.Is<GetAllRegistrationsQuery>(q => q.retreatId == retreatId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

       
        var result = await _controller.List(retreatId, null, null, 0, 20);
        
        var ok = result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.Value.Should().BeSameAs(response);
    }
}
