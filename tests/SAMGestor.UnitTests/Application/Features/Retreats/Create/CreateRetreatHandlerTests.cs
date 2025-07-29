using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Retreats.Create;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;


namespace SAMGestor.UnitTests.Application.Features.Retreats.Create
{
    public class CreateRetreatHandlerTests
    {
        private readonly Mock<IRetreatRepository> _repo;
        private readonly Mock<IUnitOfWork>        _uow;
        private readonly CreateRetreatHandler     _handler;

        public CreateRetreatHandlerTests()
        {
            _repo    = new Mock<IRetreatRepository>();
            _uow     = new Mock<IUnitOfWork>();
            _handler = new CreateRetreatHandler(_repo.Object, _uow.Object);
        }

        private CreateRetreatCommand NewCommand() =>
            new CreateRetreatCommand(
                new FullName("Handler Test"),
                "Edition1",
                "Theme",
                new DateOnly(2025,1,1),
                new DateOnly(2025,1,2),
                5, 5,
                new DateOnly(2025,1,1),
                new DateOnly(2025,1,2),
                new Money(100, "BRL"),
                new Money(50, "BRL"),
                new Percentage(60),
                new Percentage(40)
            );

        [Fact]
        public async Task Handle_Should_Create_And_ReturnResponse_When_Valid()
        {
            // Arrange
            var cmd = NewCommand();
            _repo.Setup(r => r.ExistsByNameEditionAsync(cmd.Name, cmd.Edition, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);

            // Act
            var response = await _handler.Handle(cmd, CancellationToken.None);

            // Assert
            response.Should().NotBeNull();
            response.RetreatId.Should().NotBeEmpty();

            _repo.Verify(r => r.AddAsync(It.Is<Retreat>(rt =>
                   rt.Name.Value == cmd.Name.Value &&
                   rt.Edition    == cmd.Edition), 
                It.IsAny<CancellationToken>()), Times.Once);

            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_ThrowBusinessRuleException_When_Duplicate()
        {
            // Arrange
            var cmd = NewCommand();
            _repo.Setup(r => r.ExistsByNameEditionAsync(cmd.Name, cmd.Edition, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

            // Act
            Func<Task<CreateRetreatResponse>> act = () => _handler.Handle(cmd, CancellationToken.None);

            // Assert
            await act
                .Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("Retreat already exists.");

            _repo.Verify(r => r.AddAsync(It.IsAny<Retreat>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
