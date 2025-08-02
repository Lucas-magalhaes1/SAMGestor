using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Retreats.Delete;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;
using Xunit;

namespace SAMGestor.UnitTests.Application.Features.Retreats.Delete
{
    public class DeleteRetreatHandlerTests
    {
        private readonly Mock<IRetreatRepository> _repo = new();
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly DeleteRetreatHandler _handler;

        public DeleteRetreatHandlerTests()
        {
            _handler = new DeleteRetreatHandler(_repo.Object, _uow.Object);
        }

        private static Retreat BuildRetreat(Guid id)
        {
            return new Retreat(
                new FullName("Delete Teste"),
                "2031",
                "Tema",
                new DateOnly(2031, 1, 1),
                new DateOnly(2031, 1, 2),
                1, 1,
                new DateOnly(2030, 12, 1),
                new DateOnly(2030, 12, 2),
                new Money(100, "BRL"),
                new Money(50, "BRL"),
                new Percentage(50),
                new Percentage(50)
            );
        }
        
        [Fact]
        public async Task Handle_Should_Remove_And_Commit_When_Found()
        {
            // Arrange
            var id = Guid.NewGuid();
            var retreat = BuildRetreat(id);

            _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(retreat);

            var cmd = new DeleteRetreatCommand(id);

            // Act
            var resp = await _handler.Handle(cmd, CancellationToken.None);

            // Assert
            resp.Id.Should().Be(id);

            _repo.Verify(r => r.RemoveAsync(retreat, It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Throw_NotFound_When_Entity_Missing()
        {
            // Arrange
            var id = Guid.NewGuid();
            _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Retreat?)null);

            var cmd = new DeleteRetreatCommand(id);

            // Act
            Func<Task> act = () => _handler.Handle(cmd, CancellationToken.None);

            // Assert
            await act.Should()
                     .ThrowAsync<NotFoundException>()
                     .WithMessage($"*{id}*");

            _repo.Verify(r => r.RemoveAsync(It.IsAny<Retreat>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
