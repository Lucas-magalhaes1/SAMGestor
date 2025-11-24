using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SAMGestor.API.Controllers;
using SAMGestor.API.Controllers.Family;
using SAMGestor.Application.Features.Families.Groups;
using SAMGestor.Application.Features.Families.Groups.Create;
using SAMGestor.Application.Features.Families.Groups.Notify;

namespace SAMGestor.UnitTests.API.Controllers;

public class AdminFamilyGroupsControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly AdminFamilyGroupsController _controller;

    public AdminFamilyGroupsControllerTests()
    {
        _controller = new AdminFamilyGroupsController(_mediator.Object);
    }

    [Fact]
    public async Task CreateAllGroups_retorna_202_Accepted_com_resposta_e_propagando_RetreatId_da_rota()
    {
        var retreatId = Guid.NewGuid();

        var expected = new CreateFamilyGroupsResponse(
            TotalFamilies: 10,
            Queued: 7,
            Skipped: 3
        );

        _mediator
            .Setup(m => m.Send(It.Is<CreateFamilyGroupsCommand>(c =>
                    c.RetreatId == retreatId &&
                    c.DryRun == true &&
                    c.ForceRecreate == false),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var body = new CreateFamilyGroupsCommand(
            RetreatId: Guid.Empty, 
            DryRun: true,
            ForceRecreate: false
        );

        var result = await _controller.CreateAllGroups(retreatId, body, default);

        var accepted = result.Result as AcceptedResult;
        accepted.Should().NotBeNull();
        accepted!.StatusCode.Should().Be(202);
        accepted.Value.Should().BeSameAs(expected);

        _mediator.Verify(m => m.Send(It.IsAny<CreateFamilyGroupsCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyFamilyGroup_retorna_202_Accepted_com_resposta_e_propagando_ids_da_rota()
    {
        var retreatId = Guid.NewGuid();
        var familyId  = Guid.NewGuid();

        var expected = new NotifyFamilyGroupResponse(
            Queued: true,
            Skipped: false,
            Version: 5
        );

        _mediator
            .Setup(m => m.Send(It.Is<NotifyFamilyGroupCommand>(c =>
                    c.RetreatId == retreatId &&
                    c.FamilyId  == familyId &&
                    c.ForceRecreate == true),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var body = new NotifyFamilyGroupCommand(
            RetreatId: Guid.Empty, 
            FamilyId: Guid.Empty, 
            ForceRecreate: true
        );

        var result = await _controller.NotifyFamilyGroup(retreatId, familyId, body, default);

        var accepted = result.Result as AcceptedResult;
        accepted.Should().NotBeNull();
        accepted!.StatusCode.Should().Be(202);
        accepted.Value.Should().BeSameAs(expected);

        _mediator.Verify(m => m.Send(It.IsAny<NotifyFamilyGroupCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
