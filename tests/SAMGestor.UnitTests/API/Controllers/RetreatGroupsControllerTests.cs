using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SAMGestor.API.Controllers;
using SAMGestor.Application.Features.Families.Groups.ListByStatus;
using SAMGestor.Application.Features.Families.Groups.Resend;
using SAMGestor.Application.Features.Families.Groups.RetryFailed;
using SAMGestor.Application.Features.Families.Groups.Status;

namespace SAMGestor.UnitTests.API.Controllers;

public class RetreatGroupsControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly RetreatGroupsController _controller;

    public RetreatGroupsControllerTests()
    {
        _controller = new RetreatGroupsController(_mediator.Object);
    }

    [Fact]
    public async Task GetStatusSummary_returns_200_with_response()
    {
        var retreatId = Guid.NewGuid();
        var expected = new GetGroupsStatusSummaryResponse(
            TotalFamilies: 10, None: 2, Creating: 3, Active: 4, Failed: 1);

        _mediator
            .Setup(m => m.Send(It.Is<GetGroupsStatusSummaryQuery>(q => q.RetreatId == retreatId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.GetStatusSummary(retreatId, default);

        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(200);
        ok.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task ListByStatus_returns_200_and_propagates_status_query()
    {
        var retreatId = Guid.NewGuid();
        var status = "active";
        var expected = new ListFamiliesByGroupStatusResponse(new List<FamilyGroupItem>());

        _mediator
            .Setup(m => m.Send(It.Is<ListFamiliesByGroupStatusQuery>(q => q.RetreatId == retreatId && q.Status == status), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.ListByStatus(retreatId, status, default);

        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.StatusCode.Should().Be(200);
        ok.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task ListByStatus_returns_200_when_status_null()
    {
        var retreatId = Guid.NewGuid();
        var expected = new ListFamiliesByGroupStatusResponse(new List<FamilyGroupItem>());

        _mediator
            .Setup(m => m.Send(It.Is<ListFamiliesByGroupStatusQuery>(q => q.RetreatId == retreatId && q.Status == null), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.ListByStatus(retreatId, null, default);

        var ok = result.Result as OkObjectResult;
        ok.Should().NotBeNull();
        ok!.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task Resend_returns_202_when_queued_true()
    {
        var retreatId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var expected = new ResendFamilyGroupResponse(Queued: true, Reason: null);

        _mediator
            .Setup(m => m.Send(It.Is<ResendFamilyGroupCommand>(c => c.RetreatId == retreatId && c.FamilyId == familyId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.Resend(retreatId, familyId, default);

        var accepted = result.Result as AcceptedResult;
        accepted.Should().NotBeNull();
        accepted!.StatusCode.Should().Be(202);
        accepted.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task Resend_returns_400_when_queued_false_including_reason_body()
    {
        var retreatId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var expected = new ResendFamilyGroupResponse(Queued: false, Reason: "NO_GROUP_LINK");

        _mediator
            .Setup(m => m.Send(It.Is<ResendFamilyGroupCommand>(c => c.RetreatId == retreatId && c.FamilyId == familyId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.Resend(retreatId, familyId, default);

        var bad = result.Result as BadRequestObjectResult;
        bad.Should().NotBeNull();
        bad!.StatusCode.Should().Be(400);
        bad.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task RetryFailed_returns_202_with_response()
    {
        var retreatId = Guid.NewGuid();
        var expected = new RetryFailedGroupsResponse(TotalFailed: 5, Queued: 4, Skipped: 1);

        _mediator
            .Setup(m => m.Send(It.Is<RetryFailedGroupsCommand>(c => c.RetreatId == retreatId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.RetryFailed(retreatId, default);

        var accepted = result.Result as AcceptedResult;
        accepted.Should().NotBeNull();
        accepted!.StatusCode.Should().Be(202);
        accepted.Value.Should().BeSameAs(expected);
    }
}
