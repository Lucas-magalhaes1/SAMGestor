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
    public class LockAllServiceSpacesHandlerTests
    {
        private readonly Mock<IRetreatRepository> _retreatRepo = new();
        private readonly Mock<IServiceSpaceRepository> _spaceRepo = new();
        private readonly Mock<IUnitOfWork> _uow = new();

        private LockAllServiceSpacesHandler Handler()
            => new LockAllServiceSpacesHandler(_retreatRepo.Object, _spaceRepo.Object, _uow.Object);

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
            var cmd = new LockAllServiceSpacesCommand(Guid.NewGuid(), Lock: true);
            _retreatRepo.Setup(r => r.GetByIdAsync(cmd.RetreatId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Retreat?)null);

            await FluentActions.Invoking(() => Handler().Handle(cmd, default))
                .Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Retreat*");
        }

        [Fact]
        public async Task Lock_all_updates_only_unlocked_spaces_bumps_version_and_saves()
        {
            var retreat = OpenRetreat();
            var s1 = Space(retreat.Id, "Apoio", locked: false);
            var s2 = Space(retreat.Id, "Cozinha", locked: true);
            var s3 = Space(retreat.Id, "Capela", locked: false);

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.ListByRetreatAsync(retreat.Id, default)).ReturnsAsync(new List<ServiceSpace> { s1, s2, s3 });
            _spaceRepo.Setup(s => s.UpdateAsync(It.IsAny<ServiceSpace>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _retreatRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var prev = retreat.ServiceSpacesVersion;
            var res = await Handler().Handle(new LockAllServiceSpacesCommand(retreat.Id, Lock: true), default);

            res.ChangedCount.Should().Be(2);
            retreat.ServiceSpacesVersion.Should().Be(prev + 1);
            res.Version.Should().Be(retreat.ServiceSpacesVersion);

            _spaceRepo.Verify(s => s.UpdateAsync(It.Is<ServiceSpace>(x => x.Id == s1.Id && x.IsLocked), It.IsAny<CancellationToken>()), Times.Once);
            _spaceRepo.Verify(s => s.UpdateAsync(It.Is<ServiceSpace>(x => x.Id == s3.Id && x.IsLocked), It.IsAny<CancellationToken>()), Times.Once);
            _spaceRepo.Verify(s => s.UpdateAsync(It.Is<ServiceSpace>(x => x.Id == s2.Id), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Unlock_all_updates_only_locked_spaces_bumps_version_and_saves()
        {
            var retreat = OpenRetreat();
            var s1 = Space(retreat.Id, "Apoio", locked: true);
            var s2 = Space(retreat.Id, "Cozinha", locked: true);
            var s3 = Space(retreat.Id, "Capela", locked: false);

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.ListByRetreatAsync(retreat.Id, default)).ReturnsAsync(new List<ServiceSpace> { s1, s2, s3 });
            _spaceRepo.Setup(s => s.UpdateAsync(It.IsAny<ServiceSpace>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _retreatRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var prev = retreat.ServiceSpacesVersion;
            var res = await Handler().Handle(new LockAllServiceSpacesCommand(retreat.Id, Lock: false), default);

            res.ChangedCount.Should().Be(2);
            retreat.ServiceSpacesVersion.Should().Be(prev + 1);
            res.Version.Should().Be(retreat.ServiceSpacesVersion);

            _spaceRepo.Verify(s => s.UpdateAsync(It.Is<ServiceSpace>(x => x.Id == s1.Id && !x.IsLocked), It.IsAny<CancellationToken>()), Times.Once);
            _spaceRepo.Verify(s => s.UpdateAsync(It.Is<ServiceSpace>(x => x.Id == s2.Id && !x.IsLocked), It.IsAny<CancellationToken>()), Times.Once);
            _spaceRepo.Verify(s => s.UpdateAsync(It.Is<ServiceSpace>(x => x.Id == s3.Id), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task No_changes_no_bump_and_no_save()
        {
            var retreat = OpenRetreat();
            var s1 = Space(retreat.Id, "Apoio", locked: true);
            var s2 = Space(retreat.Id, "Cozinha", locked: true);

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.ListByRetreatAsync(retreat.Id, default)).ReturnsAsync(new List<ServiceSpace> { s1, s2 });

            var prev = retreat.ServiceSpacesVersion;
            var res = await Handler().Handle(new LockAllServiceSpacesCommand(retreat.Id, Lock: true), default);

            res.ChangedCount.Should().Be(0);
            retreat.ServiceSpacesVersion.Should().Be(prev);
            _spaceRepo.Verify(s => s.UpdateAsync(It.IsAny<ServiceSpace>(), It.IsAny<CancellationToken>()), Times.Never);
            _retreatRepo.Verify(r => r.UpdateAsync(It.IsAny<Retreat>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
