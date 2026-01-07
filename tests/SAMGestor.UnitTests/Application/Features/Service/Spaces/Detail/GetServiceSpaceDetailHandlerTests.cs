using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Service.Spaces.Detail;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;
using System.Reflection;

namespace SAMGestor.UnitTests.Application.Features.Service.Spaces.Detail
{
    public class GetServiceSpaceDetailHandlerTests
    {
        private readonly Mock<IRetreatRepository> _retreatRepo = new();
        private readonly Mock<IServiceSpaceRepository> _spaceRepo = new();
        private readonly Mock<IServiceAssignmentRepository> _assignRepo = new();
        private readonly Mock<IServiceRegistrationRepository> _regRepo = new();

        private GetServiceSpaceDetailHandler Handler()
            => new GetServiceSpaceDetailHandler(_retreatRepo.Object, _spaceRepo.Object, _assignRepo.Object,
                _regRepo.Object);

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

        private static ServiceSpace Space(Guid retreatId, string name, int min = 0, int max = 10, bool isActive = true,
            bool isLocked = false)
        {
            var s = new ServiceSpace(retreatId, name, description: null, maxPeople: max, minPeople: min);
            if (!isActive)
            {
                var f = typeof(ServiceSpace).GetField("<IsActive>k__BackingField",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                f?.SetValue(s, false);
            }

            if (isLocked)
            {
                var f = typeof(ServiceSpace).GetField("<IsLocked>k__BackingField",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                f?.SetValue(s, true);
            }

            return s;
        }

        private static ServiceAssignment Assign(Guid spaceId, Guid regId, ServiceRole role)
            => new ServiceAssignment(spaceId, regId, role);

        private static ServiceRegistration Reg(Guid retreatId, string name, string emailUser, string cpf)
            => new ServiceRegistration(
                retreatId,
                new FullName(name),
                new CPF(cpf),
                new EmailAddress($"{emailUser}@mail.com"),
                "11999999999",
                new DateOnly(1990, 1, 1),
                Gender.Male,
                "SP",
                "Oeste");

        [Fact]
        public async Task Throw_NotFound_when_retreat_missing()
        {
            var q = new GetServiceSpaceDetailQuery(Guid.NewGuid(), Guid.NewGuid());
            _retreatRepo.Setup(r => r.GetByIdAsync(q.RetreatId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Retreat?)null);

            await FluentActions.Invoking(() => Handler().Handle(q, default))
                .Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Retreat*");
        }

        [Fact]
        public async Task Throw_NotFound_when_space_missing()
        {
            var retreat = OpenRetreat();
            var q = new GetServiceSpaceDetailQuery(retreat.Id, Guid.NewGuid());

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.GetByIdAsync(q.SpaceId, default)).ReturnsAsync((ServiceSpace?)null);

            await FluentActions.Invoking(() => Handler().Handle(q, default))
                .Should().ThrowAsync<NotFoundException>()
                .WithMessage("*ServiceSpace*");
        }

        [Fact]
        public async Task Throw_when_space_belongs_to_other_retreat()
        {
            var retreat = OpenRetreat();
            var other = OpenRetreat();
            var space = Space(other.Id, "Apoio");

            var q = new GetServiceSpaceDetailQuery(retreat.Id, space.Id);

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.GetByIdAsync(space.Id, default)).ReturnsAsync(space);

            await FluentActions.Invoking(() => Handler().Handle(q, default))
                .Should().ThrowAsync<BusinessRuleException>()
                .WithMessage("Space does not belong to this retreat.");
        }

        [Fact]
        public async Task Return_sorted_members_with_flags_and_space_view()
        {
            var retreat = OpenRetreat();
            var space = Space(retreat.Id, "Cozinha", min: 1, max: 5, isActive: true, isLocked: false);

            var rCoord = Reg(retreat.Id, "Alice Souza", "alice", "52998224725");
            var rVice = Reg(retreat.Id, "Bruno Lima", "bruno", "52998224726");
            var rA = Reg(retreat.Id, "Carla Mendes", "carla", "52998224727");
            var rB = Reg(retreat.Id, "Daniel Alves", "daniel", "52998224728");

            var assigns = new List<ServiceAssignment>
            {
                Assign(space.Id, rA.Id, ServiceRole.Member),
                Assign(space.Id, rCoord.Id, ServiceRole.Coordinator),
                Assign(space.Id, rB.Id, ServiceRole.Member),
                Assign(space.Id, rVice.Id, ServiceRole.Vice),
            };

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.GetByIdAsync(space.Id, default)).ReturnsAsync(space);
            _assignRepo.Setup(a => a.ListBySpaceIdsAsync(It.IsAny<Guid[]>(), default)).ReturnsAsync(assigns);
            _regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<Guid[]>(), default))
                .ReturnsAsync(new Dictionary<Guid, ServiceRegistration>
                {
                    [rCoord.Id] = rCoord, [rVice.Id] = rVice, [rA.Id] = rA, [rB.Id] = rB
                });
            
            var q = new GetServiceSpaceDetailQuery(retreat.Id, space.Id, Search: null, Skip: 0, Take: 50);
            var res = await Handler().Handle(q, default);

            res.Version.Should().Be(retreat.ServiceSpacesVersion);
            res.Space.SpaceId.Should().Be(space.Id);
            res.Space.Name.Should().Be("Cozinha");
            res.Space.IsActive.Should().BeTrue();
            res.Space.IsLocked.Should().BeFalse();
            res.Space.MinPeople.Should().Be(1);
            res.Space.MaxPeople.Should().Be(5);
            res.Space.HasCoordinator.Should().BeTrue();
            res.Space.HasVice.Should().BeTrue();
            res.Space.Allocated.Should().Be(4);

           
            res.Members.TotalCount.Should().Be(4);
            res.Members.Items.Should().HaveCount(4);
            res.Members.Items.Select(m => m.Name).Should()
                .Equal("Alice Souza", "Bruno Lima", "Carla Mendes", "Daniel Alves");
            res.Members.Items.Select(m => m.Role).Should().Equal(
                nameof(ServiceRole.Coordinator),
                nameof(ServiceRole.Vice),
                nameof(ServiceRole.Member),
                nameof(ServiceRole.Member)
            );
        }

        [Fact]
        public async Task Apply_search_filter_on_name_email_and_cpf()
        {
            var retreat = OpenRetreat();
            var space = Space(retreat.Id, "Apoio");

            var r1 = Reg(retreat.Id, "João Silva", "joao.s", "11111111111");
            var r2 = Reg(retreat.Id, "Pedro Souza", "pedro.s", "22222222222");
            var r3 = Reg(retreat.Id, "Ana Lima", "ana.l", "33333333333");

            var assigns = new List<ServiceAssignment>
            {
                Assign(space.Id, r1.Id, ServiceRole.Member),
                Assign(space.Id, r2.Id, ServiceRole.Member),
                Assign(space.Id, r3.Id, ServiceRole.Member),
            };

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.GetByIdAsync(space.Id, default)).ReturnsAsync(space);
            _assignRepo.Setup(a => a.ListBySpaceIdsAsync(It.IsAny<Guid[]>(), default)).ReturnsAsync(assigns);
            _regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<Guid[]>(), default))
                .ReturnsAsync(new Dictionary<Guid, ServiceRegistration>
                {
                    [r1.Id] = r1, [r2.Id] = r2, [r3.Id] = r3
                });

           
            var q1 = new GetServiceSpaceDetailQuery(retreat.Id, space.Id, "ana", 0, 50);
            var rName = await Handler().Handle(q1, default);
            rName.Members.Items.Should().ContainSingle(m => m.Name == "Ana Lima");

            var q2 = new GetServiceSpaceDetailQuery(retreat.Id, space.Id, "pedro.s@mail.com", 0, 50);
            var rEmail = await Handler().Handle(q2, default);
            rEmail.Members.Items.Should().ContainSingle(m => m.Email == "pedro.s@mail.com");

            var q3 = new GetServiceSpaceDetailQuery(retreat.Id, space.Id, "11111111111", 0, 50);
            var rCpf = await Handler().Handle(q3, default);
            rCpf.Members.Items.Should().ContainSingle(m => m.Cpf == "11111111111");
        }

        [Fact]
        public async Task Paginate_correctly_with_skip_and_take()
        {
            var retreat = OpenRetreat();
            var space = Space(retreat.Id, "Secretaria");

            var regs = Enumerable.Range(0, 230)
                .Select(i => Reg(retreat.Id, $"Nome Sobrenome{i}", $"u{i}", $"4{i:0000000000}"))
                .ToList();
            var assigns = regs.Select(r => Assign(space.Id, r.Id, ServiceRole.Member)).ToList();

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.GetByIdAsync(space.Id, default)).ReturnsAsync(space);
            _assignRepo.Setup(a => a.ListBySpaceIdsAsync(It.IsAny<Guid[]>(), default)).ReturnsAsync(assigns);
            _regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<Guid[]>(), default))
                .ReturnsAsync(regs.ToDictionary(x => x.Id, x => x));

           
            var q1 = new GetServiceSpaceDetailQuery(retreat.Id, space.Id, Search: null, Skip: 0, Take: 50);
            var r1 = await Handler().Handle(q1, default);
            r1.Members.Skip.Should().Be(0);
            r1.Members.Take.Should().Be(50);
            r1.Members.Items.Count.Should().Be(50);
            r1.Members.TotalCount.Should().Be(230);
            r1.Members.HasNextPage.Should().BeTrue();

            
            var q2 = new GetServiceSpaceDetailQuery(retreat.Id, space.Id, Search: null, Skip: 0, Take: 0);
            var r2 = await Handler().Handle(q2, default);
            r2.Members.Items.Count.Should().Be(230);
            r2.Members.TotalCount.Should().Be(230);
            r2.Members.HasNextPage.Should().BeFalse();

            
            var q3 = new GetServiceSpaceDetailQuery(retreat.Id, space.Id, Search: null, Skip: 50, Take: 50);
            var r3 = await Handler().Handle(q3, default);
            r3.Members.Skip.Should().Be(50);
            r3.Members.Items.Count.Should().Be(50);
            r3.Members.HasPreviousPage.Should().BeTrue();
        }
    }
}
