using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Service.Spaces.List;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;
using System.Reflection;

namespace SAMGestor.UnitTests.Application.Features.Service.Spaces.List
{
    public class ListServiceSpacesHandlerTests
    {
        private readonly Mock<IRetreatRepository> _retreatRepo = new();
        private readonly Mock<IServiceSpaceRepository> _spaceRepo = new();
        private readonly Mock<IServiceAssignmentRepository> _assignRepo = new();

        private ListServiceSpacesHandler Handler()
            => new ListServiceSpacesHandler(_retreatRepo.Object, _spaceRepo.Object, _assignRepo.Object);

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

        private static ServiceSpace Space(Guid retreatId, string name, string? desc, int min, int max, bool active = true, bool locked = false)
        {
            var s = new ServiceSpace(retreatId, name, desc, max, min);
            if (!active) s.Deactivate();
            if (locked)
            {
                var f = typeof(ServiceSpace).GetField("<IsLocked>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
                f?.SetValue(s, true);
            }
            return s;
        }

        private static ServiceAssignment Assign(Guid spaceId)
            => new ServiceAssignment(spaceId, Guid.NewGuid(), ServiceRole.Member);

        [Fact]
        public async Task Throw_NotFound_when_retreat_missing()
        {
            var q = new ListServiceSpacesQuery(Guid.NewGuid());
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

            var res = await Handler().Handle(new ListServiceSpacesQuery(retreat.Id), default);

            res.Version.Should().Be(retreat.ServiceSpacesVersion);
            res.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task Filters_by_active_locked_search_and_sorts_by_name_and_counts_allocated()
        {
            var retreat = OpenRetreat();
            var s1 = Space(retreat.Id, "Apoio", "desc 1", 1, 3, active: true, locked: false);
            var s2 = Space(retreat.Id, "Capela", "Equipe capela", 0, 2, active: false, locked: true);
            var s3 = Space(retreat.Id, "Cozinha", "preparo de alimentos", 2, 5, active: true, locked: false);

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.ListByRetreatAsync(retreat.Id, default)).ReturnsAsync(new List<ServiceSpace> { s1, s2, s3 });

            var assigns = new List<ServiceAssignment>
            {
                Assign(s1.Id),
                Assign(s1.Id),
                Assign(s3.Id)
            };
            _assignRepo.Setup(a => a.ListBySpaceIdsAsync(It.IsAny<Guid[]>(), default)).ReturnsAsync(assigns);

            var q = new ListServiceSpacesQuery(retreat.Id, IsActive: true, IsLocked: false, Search: "co");
            var res = await Handler().Handle(q, default);

            res.Items.Should().HaveCount(1);
            var only = res.Items.Single();
            only.SpaceId.Should().Be(s3.Id);
            only.Name.Should().Be("Cozinha");
            only.Description.Should().Be("preparo de alimentos");
            only.IsActive.Should().BeTrue();
            only.IsLocked.Should().BeFalse();
            only.MinPeople.Should().Be(2);
            only.MaxPeople.Should().Be(5);
            only.Allocated.Should().Be(1);
        }

        [Fact]
        public async Task Allocated_zero_when_space_has_no_assignments()
        {
            var retreat = OpenRetreat();
            var s1 = Space(retreat.Id, "Alpha", null, 0, 1);
            var s2 = Space(retreat.Id, "Beta", null, 0, 1);

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.ListByRetreatAsync(retreat.Id, default)).ReturnsAsync(new List<ServiceSpace> { s1, s2 });
            _assignRepo.Setup(a => a.ListBySpaceIdsAsync(It.IsAny<Guid[]>(), default)).ReturnsAsync(new List<ServiceAssignment> { Assign(s2.Id) });

            var res = await Handler().Handle(new ListServiceSpacesQuery(retreat.Id), default);

            var alpha = res.Items.First(i => i.SpaceId == s1.Id);
            var beta  = res.Items.First(i => i.SpaceId == s2.Id);
            alpha.Allocated.Should().Be(0);
            beta.Allocated.Should().Be(1);
        }

        [Fact]
        public async Task Search_matches_description_case_insensitive_and_orders_by_name()
        {
            var retreat = OpenRetreat();
            var s1 = Space(retreat.Id, "Zebra", "Grupo APOIO geral", 0, 1);
            var s2 = Space(retreat.Id, "Apoio Especial", "Equipe de campo", 0, 1);
            var s3 = Space(retreat.Id, "Centro", "Sem relação", 0, 1);

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.ListByRetreatAsync(retreat.Id, default)).ReturnsAsync(new List<ServiceSpace> { s1, s2, s3 });
            _assignRepo.Setup(a => a.ListBySpaceIdsAsync(It.IsAny<Guid[]>(), default)).ReturnsAsync(new List<ServiceAssignment>());

            var res = await Handler().Handle(new ListServiceSpacesQuery(retreat.Id, Search: "apoio"), default);

            res.Items.Should().HaveCount(2);
            res.Items.Select(i => i.Name).Should().Equal("Apoio Especial", "Zebra");
        }
    }
}
