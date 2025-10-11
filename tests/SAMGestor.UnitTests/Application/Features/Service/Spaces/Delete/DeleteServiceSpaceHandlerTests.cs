using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Service.Spaces.Delete;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;
using System.Reflection;

namespace SAMGestor.UnitTests.Application.Features.Service.Spaces.Delete
{
    public class DeleteServiceSpaceHandlerTests
    {
        private readonly Mock<IRetreatRepository> _retreatRepo = new();
        private readonly Mock<IServiceSpaceRepository> _spaceRepo = new();
        private readonly Mock<IServiceAssignmentRepository> _assignmentRepo = new();
        private readonly Mock<IServiceRegistrationRepository> _regRepo = new();
        private readonly Mock<IUnitOfWork> _uow = new();

        private DeleteServiceSpaceHandler Handler()
            => new DeleteServiceSpaceHandler(_retreatRepo.Object, _spaceRepo.Object, _assignmentRepo.Object, _regRepo.Object, _uow.Object);

        private static Retreat OpenRetreat()
            => new Retreat(
                new FullName("Retiro Teste"),
                "ED1", "Tema",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12)),
                10, 10,
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
                new Money(0, "BRL"), new Money(0, "BRL"),
                new Percentage(50), new Percentage(50));

        private static ServiceSpace Space(Guid retreatId, string name, int min = 0, int max = 10, bool locked = false)
        {
            var s = new ServiceSpace(retreatId, name, description: null, maxPeople: max, minPeople: min);
            if (locked)
            {
                var f = typeof(ServiceSpace).GetField("<IsLocked>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
                f?.SetValue(s, true);
            }
            return s;
        }

        [Fact]
        public async Task Throw_NotFound_when_retreat_missing()
        {
            var cmd = new DeleteServiceSpaceCommand(Guid.NewGuid(), Guid.NewGuid());
            _retreatRepo.Setup(r => r.GetByIdAsync(cmd.RetreatId, It.IsAny<CancellationToken>())).ReturnsAsync((Retreat?)null);

            await FluentActions.Invoking(() => Handler().Handle(cmd, default))
                .Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Retreat*");
        }

        [Fact]
        public async Task Throw_NotFound_when_space_missing()
        {
            var retreat = OpenRetreat();
            var cmd = new DeleteServiceSpaceCommand(retreat.Id, Guid.NewGuid());

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.GetByIdAsync(cmd.SpaceId, default)).ReturnsAsync((ServiceSpace?)null);

            await FluentActions.Invoking(() => Handler().Handle(cmd, default))
                .Should().ThrowAsync<NotFoundException>()
                .WithMessage("*ServiceSpace*");
        }

        [Fact]
        public async Task Throw_when_space_belongs_to_other_retreat()
        {
            var retreat = OpenRetreat();
            var other = OpenRetreat();
            var space = Space(other.Id, "Apoio");

            var cmd = new DeleteServiceSpaceCommand(retreat.Id, space.Id);

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.GetByIdAsync(space.Id, default)).ReturnsAsync(space);

            await FluentActions.Invoking(() => Handler().Handle(cmd, default))
                .Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("Space não pertence a este retiro.");

            _assignmentRepo.Verify(a => a.RemoveBySpaceIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _regRepo.Verify(r => r.ClearPreferenceBySpaceIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _spaceRepo.Verify(s => s.RemoveAsync(It.IsAny<ServiceSpace>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Throw_when_space_locked()
        {
            var retreat = OpenRetreat();
            var space = Space(retreat.Id, "Cozinha", locked: true);

            var cmd = new DeleteServiceSpaceCommand(retreat.Id, space.Id);

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.GetByIdAsync(space.Id, default)).ReturnsAsync(space);

            await FluentActions.Invoking(() => Handler().Handle(cmd, default))
                .Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("Espaço bloqueado não pode ser removido.");

            _assignmentRepo.Verify(a => a.RemoveBySpaceIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _regRepo.Verify(r => r.ClearPreferenceBySpaceIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _spaceRepo.Verify(s => s.RemoveAsync(It.IsAny<ServiceSpace>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Success_delete_space_removes_assignments_clears_preferences_bumps_version_and_saves()
        {
            var retreat = OpenRetreat();
            var space = Space(retreat.Id, "Secretaria");

            var cmd = new DeleteServiceSpaceCommand(retreat.Id, space.Id);

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.GetByIdAsync(space.Id, default)).ReturnsAsync(space);
            _assignmentRepo.Setup(a => a.RemoveBySpaceIdAsync(space.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _regRepo.Setup(r => r.ClearPreferenceBySpaceIdAsync(space.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _spaceRepo.Setup(s => s.RemoveAsync(space, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _retreatRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var prevVersion = retreat.ServiceSpacesVersion;
            var res = await Handler().Handle(cmd, default);

            res.Deleted.Should().BeTrue();
            retreat.ServiceSpacesVersion.Should().Be(prevVersion + 1);
            res.Version.Should().Be(retreat.ServiceSpacesVersion);

            _assignmentRepo.Verify(a => a.RemoveBySpaceIdAsync(space.Id, It.IsAny<CancellationToken>()), Times.Once);
            _regRepo.Verify(r => r.ClearPreferenceBySpaceIdAsync(space.Id, It.IsAny<CancellationToken>()), Times.Once);
            _spaceRepo.Verify(s => s.RemoveAsync(space, It.IsAny<CancellationToken>()), Times.Once);
            _retreatRepo.Verify(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
