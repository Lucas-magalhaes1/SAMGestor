using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SAMGestor.API.Controllers;
using SAMGestor.API.Controllers.Retreat;
using SAMGestor.Application.Features.Lottery;

namespace SAMGestor.UnitTests.API.Controllers;

public class RetreatLotteryControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly RetreatLotteryController _controller;

    public RetreatLotteryControllerTests()
    {
        _controller = new RetreatLotteryController(_mediator.Object);
    }

    [Fact]
    public async Task Preview_Returns_Ok_With_Result()
    {
        var retreatId = Guid.NewGuid();
        var dto = new LotteryResultDto(new List<Guid>{Guid.NewGuid()}, new List<Guid>{Guid.NewGuid()}, 2, 1);

        _mediator.Setup(m => m.Send(It.Is<LotteryPreviewQuery>(q => q.RetreatId == retreatId), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(dto);

        var result = await _controller.Preview(retreatId, default);

        (result.Result as OkObjectResult)!.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task Commit_Returns_Ok_With_Result()
    {
        var retreatId = Guid.NewGuid();
        var dto = new LotteryResultDto([], [], 0, 0);

        _mediator.Setup(m => m.Send(It.Is<LotteryCommitCommand>(q => q.RetreatId == retreatId), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(dto);

        var result = await _controller.Commit(retreatId, default);

        (result.Result as OkObjectResult)!.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task ManualSelect_Returns_NoContent()
    {
        var retreatId = Guid.NewGuid();
        var regId = Guid.NewGuid();

        _mediator.Setup(m => m.Send(It.IsAny<ManualSelectCommand>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Unit.Value);

        var result = await _controller.ManualSelect(retreatId, regId, default);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task ManualUnselect_Returns_NoContent()
    {
        var retreatId = Guid.NewGuid();
        var regId = Guid.NewGuid();

        _mediator.Setup(m => m.Send(It.IsAny<ManualUnselectCommand>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(Unit.Value);

        var result = await _controller.ManualUnselect(retreatId, regId, default);
        result.Should().BeOfType<NoContentResult>();
    }
}
