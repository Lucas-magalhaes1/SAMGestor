using System.Reflection;
using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Service.Roster.Update;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Application.Features.Service.Rostes.Update
{
    public class UpdateServiceRosterHandlerTests
    {
        private readonly Mock<IRetreatRepository> _retreatRepo = new();
        private readonly Mock<IServiceSpaceRepository> _spaceRepo = new();
        private readonly Mock<IServiceRegistrationRepository> _regRepo = new();
        private readonly Mock<IServiceAssignmentRepository> _assignRepo = new();
        private readonly Mock<IUnitOfWork> _uow = new();

        private UpdateServiceRosterHandler Handler()
            => new UpdateServiceRosterHandler(_retreatRepo.Object, _spaceRepo.Object, _regRepo.Object, _assignRepo.Object, _uow.Object);

        private static Retreat OpenRetreat()
            => new Retreat(
                new FullName("Retiro Teste"),
                "ED1",
                "Tema",
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

        private static ServiceRegistration Reg(Guid retreatId, string name)
            => new ServiceRegistration(
                retreatId,
                new FullName(name),
                new CPF("52998224725"),
                new EmailAddress($"{Guid.NewGuid()}@mail.com"),
                "11999999999",
                new DateOnly(1990, 1, 1),
                Gender.Male,
                "SP",
                "Oeste"
            );

        private static MemberInput M(Guid regId, ServiceRole role, int pos) => new MemberInput(regId, role, pos);

        [Fact]
        public async Task NotFound_retreat()
        {
            var cmd = new UpdateServiceRosterCommand(Guid.NewGuid(), Version: 0, Spaces: Array.Empty<SpaceInput>());
            _retreatRepo.Setup(r => r.GetByIdAsync(cmd.RetreatId, It.IsAny<CancellationToken>())).ReturnsAsync((Retreat?)null);

            await FluentActions.Invoking(() => Handler().Handle(cmd, default))
                .Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Retreat*");
        }

        [Fact]
        public async Task Version_mismatch_short_circuits_and_no_persistence()
        {
            var retreat = OpenRetreat();
            var cmd = new UpdateServiceRosterCommand(retreat.Id, Version: retreat.ServiceSpacesVersion + 1, Spaces: Array.Empty<SpaceInput>());

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>())).ReturnsAsync(retreat);

            var res = await Handler().Handle(cmd, default);

            res.Errors.Should().ContainSingle(e => e.Code == "VERSION_MISMATCH");
            _spaceRepo.Verify(s => s.ListByRetreatAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _assignRepo.Verify(a => a.RemoveBySpaceIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _assignRepo.Verify(a => a.AddRangeAsync(It.IsAny<IEnumerable<ServiceAssignment>>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Unknown_space_returns_error_and_no_persistence()
        {
            var retreat = OpenRetreat();
            var s1 = Space(retreat.Id, "Apoio", 1, 3);
            var payloadUnknown = new SpaceInput(Guid.NewGuid(), null, new[] { M(Guid.NewGuid(), ServiceRole.Member, 0) });

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.ListByRetreatAsync(retreat.Id, default)).ReturnsAsync(new List<ServiceSpace> { s1 });

            var cmd = new UpdateServiceRosterCommand(retreat.Id, retreat.ServiceSpacesVersion, new[] { payloadUnknown });

            var res = await Handler().Handle(cmd, default);

            res.Errors.Should().ContainSingle(e => e.Code == "UNKNOWN_SPACE" && e.SpaceId == payloadUnknown.SpaceId);
            _assignRepo.Verify(a => a.RemoveBySpaceIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Unknown_registration_ids_return_error()
        {
            var retreat = OpenRetreat();
            var s1 = Space(retreat.Id, "Apoio", 1, 3);
            var r1 = Guid.NewGuid();

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.ListByRetreatAsync(retreat.Id, default)).ReturnsAsync(new List<ServiceSpace> { s1 });
            _regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<Guid[]>(), default)).ReturnsAsync(new Dictionary<Guid, ServiceRegistration>());

            var cmd = new UpdateServiceRosterCommand(retreat.Id, retreat.ServiceSpacesVersion, new[]
            {
                new SpaceInput(s1.Id, null, new[] { M(r1, ServiceRole.Member, 0) })
            });

            var res = await Handler().Handle(cmd, default);

            res.Errors.Should().ContainSingle(e => e.Code == "UNKNOWN_REGISTRATION" && e.RegistrationIds.Contains(r1));
        }

        [Fact]
        public async Task Wrong_retreat_on_registration_returns_error()
        {
            var retreat = OpenRetreat();
            var other = OpenRetreat();
            var s1 = Space(retreat.Id, "Apoio", 1, 3);
            var reg = Reg(other.Id, "João Silva");

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.ListByRetreatAsync(retreat.Id, default)).ReturnsAsync(new List<ServiceSpace> { s1 });
            _regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<Guid[]>(), default)).ReturnsAsync(new Dictionary<Guid, ServiceRegistration>
            {
                [reg.Id] = reg
            });

            var cmd = new UpdateServiceRosterCommand(retreat.Id, retreat.ServiceSpacesVersion, new[]
            {
                new SpaceInput(s1.Id, null, new[] { M(reg.Id, ServiceRole.Member, 0) })
            });

            var res = await Handler().Handle(cmd, default);

            res.Errors.Should().ContainSingle(e => e.Code == "WRONG_RETREAT" && e.RegistrationIds.Contains(reg.Id));
        }

        [Fact]
        public async Task Locked_space_with_members_returns_error()
        {
            var retreat = OpenRetreat();
            var s1 = Space(retreat.Id, "Apoio", 1, 3, locked: true);
            var reg = Reg(retreat.Id, "João Silva");

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.ListByRetreatAsync(retreat.Id, default)).ReturnsAsync(new List<ServiceSpace> { s1 });
            _regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<Guid[]>(), default)).ReturnsAsync(new Dictionary<Guid, ServiceRegistration>
            {
                [reg.Id] = reg
            });

            var cmd = new UpdateServiceRosterCommand(retreat.Id, retreat.ServiceSpacesVersion, new[]
            {
                new SpaceInput(s1.Id, null, new[] { M(reg.Id, ServiceRole.Member, 0) })
            });

            var res = await Handler().Handle(cmd, default);

            res.Errors.Should().ContainSingle(e => e.Code == "SPACE_LOCKED" && e.SpaceId == s1.Id);
            _assignRepo.Verify(a => a.RemoveBySpaceIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Duplicate_registration_across_spaces_returns_error()
        {
            var retreat = OpenRetreat();
            var s1 = Space(retreat.Id, "Apoio", 1, 3);
            var s2 = Space(retreat.Id, "Cozinha", 1, 3);
            var reg = Reg(retreat.Id, "João Silva");

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.ListByRetreatAsync(retreat.Id, default)).ReturnsAsync(new List<ServiceSpace> { s1, s2 });
            _regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<Guid[]>(), default)).ReturnsAsync(new Dictionary<Guid, ServiceRegistration>
            {
                [reg.Id] = reg
            });

            var cmd = new UpdateServiceRosterCommand(retreat.Id, retreat.ServiceSpacesVersion, new[]
            {
                new SpaceInput(s1.Id, null, new[] { M(reg.Id, ServiceRole.Member, 0) }),
                new SpaceInput(s2.Id, null, new[] { M(reg.Id, ServiceRole.Member, 0) })
            });

            var res = await Handler().Handle(cmd, default);

            res.Errors.Should().ContainSingle(e => e.Code == "DUPLICATE_REGISTRATION" && e.RegistrationIds.Contains(reg.Id));
            _assignRepo.Verify(a => a.RemoveBySpaceIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Duplicate_leaders_per_space_returns_error_for_coord_and_vice()
        {
            var retreat = OpenRetreat();
            var s1 = Space(retreat.Id, "Apoio", 1, 3);
            var s2 = Space(retreat.Id, "Cozinha", 1, 3);
            var r1 = Reg(retreat.Id, "João Silva");
            var r2 = Reg(retreat.Id, "Pedro Souza");
            var r3 = Reg(retreat.Id, "Ana Lima");
            var r4 = Reg(retreat.Id, "Bea Rocha");

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.ListByRetreatAsync(retreat.Id, default)).ReturnsAsync(new List<ServiceSpace> { s1, s2 });
            _regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<Guid[]>(), default)).ReturnsAsync(new Dictionary<Guid, ServiceRegistration>
            {
                [r1.Id]=r1,[r2.Id]=r2,[r3.Id]=r3,[r4.Id]=r4
            });

            var cmd = new UpdateServiceRosterCommand(retreat.Id, retreat.ServiceSpacesVersion, new[]
            {
                new SpaceInput(s1.Id, null, new[]
                {
                    M(r1.Id, ServiceRole.Coordinator, 0),
                    M(r2.Id, ServiceRole.Coordinator, 1)
                }),
                new SpaceInput(s2.Id, null, new[]
                {
                    M(r3.Id, ServiceRole.Vice, 0),
                    M(r4.Id, ServiceRole.Vice, 1)
                })
            });

            var res = await Handler().Handle(cmd, default);

            res.Errors.Should().HaveCount(2);
            res.Errors.Should().Contain(e => e.Code == "DUPLICATE_LEADER" && e.SpaceId == s1.Id && e.RegistrationIds.Contains(r1.Id) && e.RegistrationIds.Contains(r2.Id));
            res.Errors.Should().Contain(e => e.Code == "DUPLICATE_LEADER" && e.SpaceId == s2.Id && e.RegistrationIds.Contains(r3.Id) && e.RegistrationIds.Contains(r4.Id));
            _assignRepo.Verify(a => a.RemoveBySpaceIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Persists_changes_orders_by_position_and_returns_warnings_when_missing_roles_and_below_min()
        {
            var retreat = OpenRetreat();
            var s1 = Space(retreat.Id, "Apoio", min: 2, max: 2);
            var r1 = Reg(retreat.Id, "João Silva");
            var r2 = Reg(retreat.Id, "Ana Lima");

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.ListByRetreatAsync(retreat.Id, default)).ReturnsAsync(new List<ServiceSpace> { s1 });
            _regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<Guid[]>(), default)).ReturnsAsync(new Dictionary<Guid, ServiceRegistration>
            {
                [r1.Id]=r1,[r2.Id]=r2
            });

            _assignRepo.Setup(a => a.RemoveBySpaceIdAsync(s1.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            List<ServiceAssignment>? added = null;
            _assignRepo.Setup(a => a.AddRangeAsync(It.IsAny<IEnumerable<ServiceAssignment>>(), It.IsAny<CancellationToken>()))
                       .Callback<IEnumerable<ServiceAssignment>, CancellationToken>((list, _) => added = list.ToList())
                       .Returns(Task.CompletedTask);

            _retreatRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var cmd = new UpdateServiceRosterCommand(retreat.Id, retreat.ServiceSpacesVersion, new[]
            {
                new SpaceInput(s1.Id, null, new[]
                {
                    M(r2.Id, ServiceRole.Member, 1),
                    M(r1.Id, ServiceRole.Member, 0)
                })
            });

            var prevVersion = retreat.ServiceSpacesVersion;
            var res = await Handler().Handle(cmd, default);

            added!.Select(a => a.ServiceRegistrationId).Should().Equal(r1.Id, r2.Id);
            res.Spaces.Should().ContainSingle(s => s.SpaceId == s1.Id && s.Count == 2);

            res.Warnings.Select(w => w.Code).Should().Contain(new[] { "MissingCoordinator", "MissingVice" });
            res.Warnings.Select(w => w.Code).Should().NotContain(new[] { "OverMax" });
            res.Warnings.Should().NotBeEmpty();

            _assignRepo.Verify(a => a.RemoveBySpaceIdAsync(s1.Id, It.IsAny<CancellationToken>()), Times.Once);
            _assignRepo.Verify(a => a.AddRangeAsync(It.IsAny<IEnumerable<ServiceAssignment>>(), It.IsAny<CancellationToken>()), Times.Once);
            _retreatRepo.Verify(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            retreat.ServiceSpacesVersion.Should().Be(prevVersion + 1);
            res.Version.Should().Be(retreat.ServiceSpacesVersion);
        }

        [Fact]
        public async Task Persists_and_warns_overmax_without_missing_roles()
        {
            var retreat = OpenRetreat();
            var s1 = Space(retreat.Id, "Cozinha", min: 1, max: 1);
            var r1 = Reg(retreat.Id, "João Silva");
            var r2 = Reg(retreat.Id, "Ana Lima");

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.ListByRetreatAsync(retreat.Id, default)).ReturnsAsync(new List<ServiceSpace> { s1 });
            _regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<Guid[]>(), default)).ReturnsAsync(new Dictionary<Guid, ServiceRegistration>
            {
                [r1.Id]=r1,[r2.Id]=r2
            });

            _assignRepo.Setup(a => a.RemoveBySpaceIdAsync(s1.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _assignRepo.Setup(a => a.AddRangeAsync(It.Is<IEnumerable<ServiceAssignment>>(l => l.Count() == 2), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);
            _retreatRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var cmd = new UpdateServiceRosterCommand(retreat.Id, retreat.ServiceSpacesVersion, new[]
            {
                new SpaceInput(s1.Id, null, new[]
                {
                    M(r1.Id, ServiceRole.Coordinator, 0),
                    M(r2.Id, ServiceRole.Vice, 1)
                })
            }, IgnoreWarnings: true);

            var res = await Handler().Handle(cmd, default);

            res.Spaces.Single().Count.Should().Be(2);
            res.Warnings.Select(w => w.Code).Should().Contain("OverMax");
            res.Warnings.Select(w => w.Code).Should().NotContain(new[] { "MissingCoordinator", "MissingVice" });
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Persists_and_no_warnings_when_within_limits_and_roles_present()
        {
            var retreat = OpenRetreat();
            var s1 = Space(retreat.Id, "Secretaria", min: 2, max: 3);
            var r1 = Reg(retreat.Id, "João Silva");
            var r2 = Reg(retreat.Id, "Ana Lima");

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.ListByRetreatAsync(retreat.Id, default)).ReturnsAsync(new List<ServiceSpace> { s1 });
            _regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<Guid[]>(), default)).ReturnsAsync(new Dictionary<Guid, ServiceRegistration>
            {
                [r1.Id]=r1,[r2.Id]=r2
            });

            _assignRepo.Setup(a => a.RemoveBySpaceIdAsync(s1.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _assignRepo.Setup(a => a.AddRangeAsync(It.Is<IEnumerable<ServiceAssignment>>(l => l.Count() == 2), It.IsAny<CancellationToken>()))
                       .Returns(Task.CompletedTask);
            _retreatRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var cmd = new UpdateServiceRosterCommand(retreat.Id, retreat.ServiceSpacesVersion, new[]
            {
                new SpaceInput(s1.Id, null, new[]
                {
                    M(r1.Id, ServiceRole.Coordinator, 0),
                    M(r2.Id, ServiceRole.Vice, 1)
                })
            });

            var res = await Handler().Handle(cmd, default);

            res.Warnings.Should().BeEmpty();
            res.Errors.Should().BeEmpty();
            res.Spaces.Should().ContainSingle(s => s.SpaceId == s1.Id && s.Count == 2 && s.HasCoordinator && s.HasVice);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Orders_members_by_position_when_persisting_multiple()
        {
            var retreat = OpenRetreat();
            var s1 = Space(retreat.Id, "Logística", min: 0, max: 10);
            var r1 = Reg(retreat.Id, "Alice Souza");
            var r2 = Reg(retreat.Id, "Bruno Lima");
            var r3 = Reg(retreat.Id, "Carla Mendes");

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.ListByRetreatAsync(retreat.Id, default)).ReturnsAsync(new List<ServiceSpace> { s1 });
            _regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<Guid[]>(), default)).ReturnsAsync(new Dictionary<Guid, ServiceRegistration>
            {
                [r1.Id]=r1,[r2.Id]=r2,[r3.Id]=r3
            });

            _assignRepo.Setup(a => a.RemoveBySpaceIdAsync(s1.Id, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            List<ServiceAssignment>? added = null;
            _assignRepo.Setup(a => a.AddRangeAsync(It.IsAny<IEnumerable<ServiceAssignment>>(), It.IsAny<CancellationToken>()))
                       .Callback<IEnumerable<ServiceAssignment>, CancellationToken>((list, _) => added = list.ToList())
                       .Returns(Task.CompletedTask);

            _retreatRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            var cmd = new UpdateServiceRosterCommand(retreat.Id, retreat.ServiceSpacesVersion, new[]
            {
                new SpaceInput(s1.Id, null, new[]
                {
                    M(r3.Id, ServiceRole.Member, 2),
                    M(r1.Id, ServiceRole.Member, 0),
                    M(r2.Id, ServiceRole.Member, 1),
                })
            }, IgnoreWarnings: true);

            var res = await Handler().Handle(cmd, default);

            added.Should().NotBeNull();
            added!.Select(a => a.ServiceRegistrationId).Should().Equal(r1.Id, r2.Id, r3.Id);
            added!.Select(a => a.Role).Should().OnlyContain(role => role == ServiceRole.Member);

            _assignRepo.Verify(a => a.RemoveBySpaceIdAsync(s1.Id, It.IsAny<CancellationToken>()), Times.Once);
            _assignRepo.Verify(a => a.AddRangeAsync(It.IsAny<IEnumerable<ServiceAssignment>>(), It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
