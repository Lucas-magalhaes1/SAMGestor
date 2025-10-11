using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Service.Spaces.Summary;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Application.Features.Service.Spaces.Summay
{
    public class GetServiceSpacesSummaryHandlerTests
    {
        private readonly Mock<IRetreatRepository> _retreatRepo = new();
        private readonly Mock<IServiceSpaceRepository> _spaceRepo = new();
        private readonly Mock<IServiceAssignmentRepository> _assignRepo = new();
        private readonly Mock<IServiceRegistrationRepository> _regRepo = new();

        private GetServiceSpacesSummaryHandler Handler()
            => new GetServiceSpacesSummaryHandler(_retreatRepo.Object, _spaceRepo.Object, _assignRepo.Object, _regRepo.Object);

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

        private static ServiceSpace Space(Guid retreatId, string name, int min, int max, bool active = true, bool locked = false)
        {
            var s = new ServiceSpace(retreatId, name, description: null, maxPeople: max, minPeople: min);
            if (!active) s.Deactivate();
            if (locked) s.Lock();
            return s;
        }

        private static ServiceAssignment A(Guid spaceId, ServiceRole role)
            => new ServiceAssignment(spaceId, Guid.NewGuid(), role);

        [Fact]
        public async Task Throw_NotFound_when_retreat_missing()
        {
            var q = new GetServiceSpacesSummaryQuery(Guid.NewGuid());
            _retreatRepo.Setup(r => r.GetByIdAsync(q.RetreatId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Retreat?)null);

            await FluentActions.Invoking(() => Handler().Handle(q, default))
                .Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Retreat*");
        }

        [Fact]
        public async Task Return_empty_when_no_spaces()
        {
            var retreat = OpenRetreat();
            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.ListByRetreatAsync(retreat.Id, default)).ReturnsAsync(new List<ServiceSpace>());
            _assignRepo.Setup(a => a.ListBySpaceIdsAsync(It.IsAny<Guid[]>(), default)).ReturnsAsync(new List<ServiceAssignment>());
            _regRepo.Setup(r => r.CountPreferencesBySpaceAsync(retreat.Id, default)).ReturnsAsync(new Dictionary<Guid, int>());

            var res = await Handler().Handle(new GetServiceSpacesSummaryQuery(retreat.Id), default);

            res.Version.Should().Be(retreat.ServiceSpacesVersion);
            res.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task Return_summary_with_counts_roles_prefs_and_flags_sorted_by_name()
        {
            var retreat = OpenRetreat();
            var s1 = Space(retreat.Id, "Apoio",   min: 1, max: 3, active: true,  locked: false);
            var s2 = Space(retreat.Id, "Cozinha", min: 2, max: 4, active: false, locked: true);

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.ListByRetreatAsync(retreat.Id, default)).ReturnsAsync(new List<ServiceSpace> { s2, s1 });

            var assigns = new List<ServiceAssignment>
            {
                A(s1.Id, ServiceRole.Coordinator),
                A(s1.Id, ServiceRole.Member),
                A(s2.Id, ServiceRole.Member)
            };
            _assignRepo.Setup(a => a.ListBySpaceIdsAsync(It.IsAny<Guid[]>(), default)).ReturnsAsync(assigns);

            _regRepo.Setup(r => r.CountPreferencesBySpaceAsync(retreat.Id, default))
                    .ReturnsAsync(new Dictionary<Guid, int>
                    {
                        [s1.Id] = 5,
                        [s2.Id] = 0
                    });

            var res = await Handler().Handle(new GetServiceSpacesSummaryQuery(retreat.Id), default);

            res.Items.Select(i => i.Name).Should().Equal("Apoio", "Cozinha");

            var apoio = res.Items.First(i => i.SpaceId == s1.Id);
            apoio.IsActive.Should().BeTrue();
            apoio.IsLocked.Should().BeFalse();
            apoio.MinPeople.Should().Be(1);
            apoio.MaxPeople.Should().Be(3);
            apoio.Allocated.Should().Be(2);
            apoio.PreferredCount.Should().Be(5);
            apoio.HasCoordinator.Should().BeTrue();
            apoio.HasVice.Should().BeFalse();

            var cozinha = res.Items.First(i => i.SpaceId == s2.Id);
            cozinha.IsActive.Should().BeFalse();
            cozinha.IsLocked.Should().BeTrue();
            cozinha.Allocated.Should().Be(1);
            cozinha.PreferredCount.Should().Be(0);
            cozinha.HasCoordinator.Should().BeFalse();
            cozinha.HasVice.Should().BeFalse();
        }
    }
}
