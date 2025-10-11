using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Service.Spaces.BulkCapacity;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;
using System.Reflection;

namespace SAMGestor.UnitTests.Application.Features.Service.Spaces.BulkCapacity
{
    public class UpdateServiceSpacesCapacityHandlerTests
    {
        private readonly Mock<IRetreatRepository> _retreatRepo = new();
        private readonly Mock<IServiceSpaceRepository> _spaceRepo = new();
        private readonly Mock<IUnitOfWork> _uow = new();

        private UpdateServiceSpacesCapacityHandler Handler()
            => new UpdateServiceSpacesCapacityHandler(_retreatRepo.Object, _spaceRepo.Object, _uow.Object);

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

        private static ServiceSpace Space(Guid retreatId, string name, int min, int max, bool locked = false)
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
            var cmd = new UpdateServiceSpacesCapacityCommand(Guid.NewGuid(), true, 1, 5, null);
            _retreatRepo.Setup(r => r.GetByIdAsync(cmd.RetreatId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Retreat?)null);

            await FluentActions.Invoking(() => Handler().Handle(cmd, default))
                .Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Retreat*");
        }

        [Fact]
        public async Task ApplyToAll_updates_unlocked_skips_locked_and_bumps_version()
        {
            var retreat = OpenRetreat();
            var s1 = Space(retreat.Id, "Apoio", 1, 3, locked: false);
            var s2 = Space(retreat.Id, "Cozinha", 1, 3, locked: true);

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.ListByRetreatAsync(retreat.Id, default)).ReturnsAsync(new List<ServiceSpace> { s1, s2 });
            _spaceRepo.Setup(s => s.UpdateAsync(It.IsAny<ServiceSpace>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _retreatRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var prevVersion = retreat.ServiceSpacesVersion;
            var cmd = new UpdateServiceSpacesCapacityCommand(retreat.Id, true, 2, 4, null);

            var res = await Handler().Handle(cmd, default);

            res.UpdatedCount.Should().Be(1);
            res.SkippedLocked.Should().ContainSingle(x => x == s2.Id);
            retreat.ServiceSpacesVersion.Should().Be(prevVersion + 1);
            res.Version.Should().Be(retreat.ServiceSpacesVersion);
            _spaceRepo.Verify(s => s.UpdateAsync(It.Is<ServiceSpace>(x => x.Id == s1.Id && x.MinPeople == 2 && x.MaxPeople == 4), It.IsAny<CancellationToken>()), Times.Once);
            _retreatRepo.Verify(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ApplyToAll_no_changes_no_bump()
        {
            var retreat = OpenRetreat();
            var s1 = Space(retreat.Id, "Apoio", 2, 4);
            var s2 = Space(retreat.Id, "Cozinha", 2, 4);

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.ListByRetreatAsync(retreat.Id, default)).ReturnsAsync(new List<ServiceSpace> { s1, s2 });
            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var prevVersion = retreat.ServiceSpacesVersion;
            var cmd = new UpdateServiceSpacesCapacityCommand(retreat.Id, true, 2, 4, null);

            var res = await Handler().Handle(cmd, default);

            res.UpdatedCount.Should().Be(0);
            res.SkippedLocked.Should().BeEmpty();
            retreat.ServiceSpacesVersion.Should().Be(prevVersion);
            _spaceRepo.Verify(s => s.UpdateAsync(It.IsAny<ServiceSpace>(), It.IsAny<CancellationToken>()), Times.Never);
            _retreatRepo.Verify(r => r.UpdateAsync(It.IsAny<Retreat>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Items_mode_updates_changed_skips_locked_ignores_unknown_and_bumps_version()
        {
            var retreat = OpenRetreat();
            var s1 = Space(retreat.Id, "Apoio", 1, 3, locked: false);
            var s2 = Space(retreat.Id, "Cozinha", 2, 4, locked: false);
            var s3 = Space(retreat.Id, "Capela", 1, 2, locked: true);
            var unknownId = Guid.NewGuid();

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.ListByRetreatAsync(retreat.Id, default)).ReturnsAsync(new List<ServiceSpace> { s1, s2, s3 });
            _spaceRepo.Setup(s => s.UpdateAsync(It.IsAny<ServiceSpace>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _retreatRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var prevVersion = retreat.ServiceSpacesVersion;
            var cmd = new UpdateServiceSpacesCapacityCommand(
                retreat.Id, false, null, null,
                new[]
                {
                    new UpdateServiceSpacesCapacityCommand.Item(s1.Id, 3, 5),
                    new UpdateServiceSpacesCapacityCommand.Item(s2.Id, 2, 4),
                    new UpdateServiceSpacesCapacityCommand.Item(s3.Id, 5, 6),
                    new UpdateServiceSpacesCapacityCommand.Item(unknownId, 7, 8)
                });

            var res = await Handler().Handle(cmd, default);

            res.UpdatedCount.Should().Be(1);
            res.SkippedLocked.Should().ContainSingle(x => x == s3.Id);
            retreat.ServiceSpacesVersion.Should().Be(prevVersion + 1);
            _spaceRepo.Verify(s => s.UpdateAsync(It.Is<ServiceSpace>(x => x.Id == s1.Id && x.MinPeople == 3 && x.MaxPeople == 5), It.IsAny<CancellationToken>()), Times.Once);
            _spaceRepo.Verify(s => s.UpdateAsync(It.Is<ServiceSpace>(x => x.Id == s2.Id), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Items_mode_no_changes_no_bump()
        {
            var retreat = OpenRetreat();
            var s1 = Space(retreat.Id, "Apoio", 1, 3);
            var s2 = Space(retreat.Id, "Cozinha", 2, 4);

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.ListByRetreatAsync(retreat.Id, default)).ReturnsAsync(new List<ServiceSpace> { s1, s2 });
            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var prevVersion = retreat.ServiceSpacesVersion;
            var cmd = new UpdateServiceSpacesCapacityCommand(
                retreat.Id, false, null, null,
                new[]
                {
                    new UpdateServiceSpacesCapacityCommand.Item(s1.Id, 1, 3),
                    new UpdateServiceSpacesCapacityCommand.Item(s2.Id, 2, 4),
                });

            var res = await Handler().Handle(cmd, default);

            res.UpdatedCount.Should().Be(0);
            res.SkippedLocked.Should().BeEmpty();
            retreat.ServiceSpacesVersion.Should().Be(prevVersion);
            _spaceRepo.Verify(s => s.UpdateAsync(It.IsAny<ServiceSpace>(), It.IsAny<CancellationToken>()), Times.Never);
            _retreatRepo.Verify(r => r.UpdateAsync(It.IsAny<Retreat>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
