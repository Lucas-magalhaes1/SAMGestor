using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SAMGestor.API.Controllers;
using SAMGestor.Application.Features.Retreats.Create;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.ValueObjects;


namespace SAMGestor.UnitTests.API.Controllers
{
    public class RetreatsControllerTests
    {
        private readonly Mock<IMediator> _mediator;
        private readonly RetreatsController _controller;

        public RetreatsControllerTests()
        {
            _mediator  = new Mock<IMediator>();
            _controller = new RetreatsController(_mediator.Object);
        }

        [Fact]
        public async Task CreateRetreat_Returns_CreatedAtAction_When_Success()
        {
            // Arrange
            var expectedId = Guid.NewGuid();
            var command = new CreateRetreatCommand(
                new FullName("Test User"),
                "E1",
                "Theme",
                new DateOnly(2025,1,1),
                new DateOnly(2025,1,2),
                1, 1,
                new DateOnly(2025,1,1),
                new DateOnly(2025,1,2),
                new Money(0, "USD"),
                new Money(0, "USD"),
                new Percentage(50),
                new Percentage(50)
            );
            var response = new CreateRetreatResponse(expectedId);

            _mediator
                .Setup(m => m.Send(
                    It.IsAny<CreateRetreatCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.CreateRetreat(command);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            created.RouteValues.Should().ContainKey("id");
            created.RouteValues["id"].Should().Be(expectedId);
            created.Value.Should().Be(response);

            _mediator.Verify(m => m.Send(command, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateRetreat_Propagates_Exception_When_Mediator_Throws()
        {
            // Arrange
            var command = new CreateRetreatCommand(
                new FullName("Test User"),
                "E2", "Theme",
                new DateOnly(2025,1,1),
                new DateOnly(2025,1,2),
                1,1,
                new DateOnly(2025,1,1),
                new DateOnly(2025,1,2),
                new Money(0,"USD"),
                new Money(0,"USD"),
                new Percentage(50),
                new Percentage(50)
            );

            _mediator
                .Setup(m => m.Send(It.IsAny<CreateRetreatCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new BusinessRuleException("Retreat already exists."));

            // Act
            Func<Task> act = () => _controller.CreateRetreat(command);

            // Assert
            await act.Should().ThrowAsync<BusinessRuleException>()
                     .WithMessage("Retreat already exists.");
        }
    }
}
