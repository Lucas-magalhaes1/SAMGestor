using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SAMGestor.API.Controllers;
using SAMGestor.Application.Features.Retreats.Create;
using SAMGestor.Application.Features.Retreats.Delete;
using SAMGestor.Application.Features.Retreats.GetAll;
using SAMGestor.Application.Features.Retreats.GetById;
using SAMGestor.Application.Features.Retreats.Update;
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
        
        [Fact]
        public async Task Delete_Should_Return_NoContent_On_Success()
        {
            // Arrange
            var id = Guid.NewGuid();

            _mediator
                .Setup(m => m.Send(It.Is<DeleteRetreatCommand>(c => c.Id == id),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DeleteRetreatResponse(id));

            // Act
            var result = await _controller.Delete(id);

            // Assert
            result.Should().BeOfType<NoContentResult>();

            _mediator.Verify(m => m.Send(It.IsAny<DeleteRetreatCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Delete_Should_Propagate_NotFoundException()
        {
            // Arrange
            var id = Guid.NewGuid();

            _mediator
                .Setup(m => m.Send(It.Is<DeleteRetreatCommand>(c => c.Id == id),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NotFoundException("Retreat", id));

            // Act
            Func<Task> act = () => _controller.Delete(id);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }
        
        [Fact]
        public async Task List_Returns_Ok_With_Response()
        {
            // Arrange
            var response = new ListRetreatsResponse(
                new List<RetreatDto>
                {
                    new(Guid.NewGuid(),"Retiro 1","2024",
                        new DateOnly(2024,1,1),
                        new DateOnly(2024,1,2))
                },
                TotalCount: 1,
                Skip: 0,
                Take: 20);

            _mediator.Setup(m => m.Send(
                    It.Is<ListRetreatsQuery>(q => q.Skip == 0 && q.Take == 20),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.List();

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().Be(response);

            _mediator.Verify(m => m.Send(It.IsAny<ListRetreatsQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact]
        public async Task GetById_Returns_Ok_With_Response()
        {
            // Arrange
            var id = Guid.NewGuid();
            var resp = new GetRetreatByIdResponse(
                id, "Retiro front", "2030", "Tema",
                new DateOnly(2030,1,1), new DateOnly(2030,1,3),
                1,1,
                new DateOnly(2029,12,1), new DateOnly(2029,12,5),
                100, 50, 50, 50);

            _mediator.Setup(m => m.Send(
                    It.Is<GetRetreatByIdQuery>(q => q.Id == id),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(resp);

            // Act
            var result = await _controller.GetById(id);

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().Be(resp);

            _mediator.Verify(m => m.Send(
                It.IsAny<GetRetreatByIdQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetById_Propagates_NotFoundException()
        {
            // Arrange
            var id = Guid.NewGuid();

            _mediator.Setup(m => m.Send(
                    It.Is<GetRetreatByIdQuery>(q => q.Id == id),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NotFoundException("Retreat", id));

            // Act
            Func<Task> act = () => _controller.GetById(id);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }
        
        [Fact]
        public async Task Update_Returns_Ok_With_Response()
        {
            var id  = Guid.NewGuid();
            var cmd = new UpdateRetreatCommand(
                id, new FullName("Teste User"), "2035", "Tema",
                new DateOnly(2035,1,1), new DateOnly(2035,1,3),
                1,1,
                new DateOnly(2034,12,1), new DateOnly(2034,12,2),
                new Money(100,"BRL"), new Money(50,"BRL"),
                new Percentage(50), new Percentage(50));

            var expected = new UpdateRetreatResponse(id);

            _mediator.Setup(m => m.Send(It.IsAny<UpdateRetreatCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            // Act
            var result = await _controller.Update(id, cmd);

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().Be(expected);

            _mediator.Verify(m => m.Send(
                It.Is<UpdateRetreatCommand>(c => c.Id == id),
                It.IsAny<CancellationToken>()), Times.Once);
        }


        [Fact]
        public async Task Update_Propagates_NotFound()
        {
            var id  = Guid.NewGuid();
            var cmd = new UpdateRetreatCommand(
                id, new FullName("Teste User"), "2035", "Tema",
                new DateOnly(2035,1,1), new DateOnly(2035,1,3),
                1,1,
                new DateOnly(2034,12,1), new DateOnly(2034,12,2),
                new Money(100,"BRL"), new Money(50,"BRL"),
                new Percentage(50), new Percentage(50));

            _mediator.Setup(m => m.Send(It.IsAny<UpdateRetreatCommand>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NotFoundException("Retreat", id));

            Func<Task> act = () => _controller.Update(id, cmd);

            await act.Should().ThrowAsync<NotFoundException>();
        }
    }
}
