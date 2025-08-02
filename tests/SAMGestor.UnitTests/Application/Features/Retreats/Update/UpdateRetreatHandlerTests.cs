using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Retreats.Update;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;
using Xunit;

namespace SAMGestor.UnitTests.Application.Features.Retreats.Update
{
    public class UpdateRetreatHandlerTests
    {
        private readonly Mock<IRetreatRepository> _repo = new();
        private readonly Mock<IUnitOfWork>        _uow  = new();
        private readonly UpdateRetreatHandler     _handler;

        public UpdateRetreatHandlerTests()
        {
            _handler = new UpdateRetreatHandler(_repo.Object, _uow.Object);
        }

        private static Retreat Build(string name, string edition)
            => new(
                new FullName(name),
                edition,
                "Tema",
                new DateOnly(2033,1,1),
                new DateOnly(2033,1,3),
                10, 10,
                new DateOnly(2032,12,1),
                new DateOnly(2032,12,10),
                new Money(200,"BRL"),
                new Money(80,"BRL"),
                new Percentage(60),
                new Percentage(40));

        private static UpdateRetreatCommand BuildCmd(Guid id)
            => new(
                id,
                new FullName("Novo Nome"),
                "2033",
                "Tema Atualizado",
                new DateOnly(2033,1,1),
                new DateOnly(2033,1,3),
                10, 12,
                new DateOnly(2032,12,1),
                new DateOnly(2032,12,10),
                new Money(250,"BRL"),
                new Money(90,"BRL"),
                new Percentage(50),
                new Percentage(50));

        [Fact]
        public async Task Handle_Updates_When_No_Duplicate()
        {
            // Arrange
            var retreat = Build("Antigo Nome", "2032"); // cria entidade
            var id      = retreat.Id;                   // usa Id real

            _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(retreat);
            _repo.Setup(r => r.ExistsByNameEditionAsync(
                    It.IsAny<FullName>(), "2033", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var cmd = BuildCmd(id);                     // comando com o mesmo Id

            // Act
            var resp = await _handler.Handle(cmd, CancellationToken.None);

            // Assert
            resp.Id.Should().Be(id);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }


        [Fact]
        public async Task Handle_Throws_BusinessRule_When_Duplicate()
        {
            var id  = Guid.NewGuid();
            var ret = Build("Outro Retiro","2030");

            _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(ret);
            _repo.Setup(r => r.ExistsByNameEditionAsync(
                            It.IsAny<FullName>(), "2033", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

            var cmd = BuildCmd(id);

            Func<Task> act = () => _handler.Handle(cmd, CancellationToken.None);

            await act.Should().ThrowAsync<BusinessRuleException>()
                     .WithMessage("*already exists*");
        }

        [Fact]
        public async Task Handle_Throws_NotFound_When_Missing()
        {
            var id = Guid.NewGuid();
            _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Retreat?)null);

            var cmd = BuildCmd(id);

            Func<Task> act = () => _handler.Handle(cmd, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>();
        }
    }
}
