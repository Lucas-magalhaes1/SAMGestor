using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Service.Spaces.Locking;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Application.Features.Service.Spaces.Locking
{
    public class LockServiceSpaceHandlerTests
    {
        private readonly Mock<IRetreatRepository> _retreatRepo = new();
        private readonly Mock<IServiceSpaceRepository> _spaceRepo = new();
        private readonly Mock<IUnitOfWork> _uow = new();

        private LockServiceSpaceHandler Handler()
            => new LockServiceSpaceHandler(_retreatRepo.Object, _spaceRepo.Object, _uow.Object);

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

        private static ServiceSpace Space(Guid retreatId, string name, bool locked = false)
        {
            var s = new ServiceSpace(retreatId, name, description: null, maxPeople: 10, minPeople: 0);
            if (locked) s.Lock();
            return s;
        }

        [Fact]
        public async Task Throw_NotFound_when_retreat_missing()
        {
            var cmd = new LockServiceSpaceCommand(Guid.NewGuid(), Guid.NewGuid(), Lock: true);
            _retreatRepo.Setup(r => r.GetByIdAsync(cmd.RetreatId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Retreat?)null);

            await FluentActions.Invoking(() => Handler().Handle(cmd, default))
                .Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Retreat*");
        }

        [Fact]
        public async Task Throw_NotFound_when_space_missing()
        {
            var retreat = OpenRetreat();
            var cmd = new LockServiceSpaceCommand(retreat.Id, Guid.NewGuid(), Lock: true);

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
            var sp = Space(other.Id, "Apoio");

            var cmd = new LockServiceSpaceCommand(retreat.Id, sp.Id, Lock: true);

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.GetByIdAsync(sp.Id, default)).ReturnsAsync(sp);

            await FluentActions.Invoking(() => Handler().Handle(cmd, default))
                .Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("Space não pertence a este retiro.");

            _spaceRepo.Verify(s => s.UpdateAsync(It.IsAny<ServiceSpace>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Lock_when_unlocked_updates_bumps_version_and_saves()
        {
            var retreat = OpenRetreat();
            var sp = Space(retreat.Id, "Cozinha", locked: false);

            var cmd = new LockServiceSpaceCommand(retreat.Id, sp.Id, Lock: true);

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.GetByIdAsync(sp.Id, default)).ReturnsAsync(sp);
            _spaceRepo.Setup(s => s.UpdateAsync(sp, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _retreatRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var prev = retreat.ServiceSpacesVersion;
            var res = await Handler().Handle(cmd, default);

            res.Changed.Should().BeTrue();
            sp.IsLocked.Should().BeTrue();
            retreat.ServiceSpacesVersion.Should().Be(prev + 1);
            res.Version.Should().Be(retreat.ServiceSpacesVersion);

            _spaceRepo.Verify(s => s.UpdateAsync(sp, It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Unlock_when_locked_updates_bumps_version_and_saves()
        {
            var retreat = OpenRetreat();
            var sp = Space(retreat.Id, "Capela", locked: true);

            var cmd = new LockServiceSpaceCommand(retreat.Id, sp.Id, Lock: false);

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.GetByIdAsync(sp.Id, default)).ReturnsAsync(sp);
            _spaceRepo.Setup(s => s.UpdateAsync(sp, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _retreatRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var prev = retreat.ServiceSpacesVersion;
            var res = await Handler().Handle(cmd, default);

            res.Changed.Should().BeTrue();
            sp.IsLocked.Should().BeFalse();
            retreat.ServiceSpacesVersion.Should().Be(prev + 1);
            res.Version.Should().Be(retreat.ServiceSpacesVersion);

            _spaceRepo.Verify(s => s.UpdateAsync(sp, It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Noop_when_lock_state_already_matches_does_not_persist_or_bump()
        {
            var retreat = OpenRetreat();
            var sp = Space(retreat.Id, "Secretaria", locked: true);

            var cmd = new LockServiceSpaceCommand(retreat.Id, sp.Id, Lock: true);

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.GetByIdAsync(sp.Id, default)).ReturnsAsync(sp);

            var prev = retreat.ServiceSpacesVersion;
            var res = await Handler().Handle(cmd, default);

            res.Changed.Should().BeFalse();
            retreat.ServiceSpacesVersion.Should().Be(prev);

            _spaceRepo.Verify(s => s.UpdateAsync(It.IsAny<ServiceSpace>(), It.IsAny<CancellationToken>()), Times.Never);
            _retreatRepo.Verify(r => r.UpdateAsync(It.IsAny<Retreat>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
