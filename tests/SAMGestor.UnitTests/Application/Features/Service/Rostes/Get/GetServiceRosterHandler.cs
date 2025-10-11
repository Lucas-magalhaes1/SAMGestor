using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Service.Roster.Get;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Application.Features.Service.Roster.Get
{
    public class GetServiceRosterHandlerTests
    {
        private readonly Mock<IRetreatRepository> _retreatRepo = new();
        private readonly Mock<IServiceSpaceRepository> _spaceRepo = new();
        private readonly Mock<IServiceAssignmentRepository> _assignRepo = new();
        private readonly Mock<IServiceRegistrationRepository> _regRepo = new();

        private GetServiceRosterHandler Handler()
            => new GetServiceRosterHandler(_retreatRepo.Object, _spaceRepo.Object, _assignRepo.Object, _regRepo.Object);

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

        private static ServiceSpace Space(Guid retreatId, string name, int min = 0, int max = 10)
            => new ServiceSpace(retreatId, name, description: null, maxPeople: max, minPeople: min);

        private static ServiceAssignment Assign(Guid spaceId, Guid regId, ServiceRole role)
            => new ServiceAssignment(spaceId, regId, role);

        [Fact]
        public async Task Throw_NotFound_when_retreat_missing()
        {
            var q = new GetServiceRosterQuery(Guid.NewGuid());
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
            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<ServiceSpace>());

            var q = new GetServiceRosterQuery(retreat.Id);
            var res = await Handler().Handle(q, default);

            res.Version.Should().Be(retreat.ServiceSpacesVersion);
            res.Spaces.Should().BeEmpty();
        }

        [Fact]
        public async Task Return_spaces_with_members()
        {
            var retreat = OpenRetreat();
            var space1 = Space(retreat.Id, "Apoio", 1, 3);
            var space2 = Space(retreat.Id, "Cozinha", 2, 5);

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(retreat);
            _spaceRepo.Setup(s => s.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<ServiceSpace> { space1, space2 });

            var regId1 = Guid.NewGuid();
            var regId2 = Guid.NewGuid();
            var regId3 = Guid.NewGuid();
            var assignments = new List<ServiceAssignment>
            {
                Assign(space1.Id, regId1, ServiceRole.Member),
                Assign(space2.Id, regId2, ServiceRole.Coordinator),
                Assign(space2.Id, regId3, ServiceRole.Member)
            };

            _assignRepo.Setup(a => a.ListBySpaceIdsAsync(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(assignments);
            _regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Dictionary<Guid, ServiceRegistration>
                    {
                        [regId1] = new ServiceRegistration(retreat.Id, new FullName("João Silva"), new CPF("52998224725"), new EmailAddress("joao@mail.com"), "11999999999", new DateOnly(1990, 1, 1), Gender.Male, "SP", "Oeste"),
                        [regId2] = new ServiceRegistration(retreat.Id, new FullName("Pedro Souza"), new CPF("52998224726"), new EmailAddress("pedro@mail.com"), "11999999998", new DateOnly(1990, 1, 1), Gender.Male, "SP", "Oeste"),
                        [regId3] = new ServiceRegistration(retreat.Id, new FullName("Ana Lima"), new CPF("52998224727"), new EmailAddress("ana@mail.com"), "11999999997", new DateOnly(1990, 1, 1), Gender.Female, "SP", "Oeste")
                    });

            var q = new GetServiceRosterQuery(retreat.Id);
            var res = await Handler().Handle(q, default);

            res.Version.Should().Be(retreat.ServiceSpacesVersion);
            res.Spaces.Should().HaveCount(2);

            var apoios = res.Spaces.First(s => s.SpaceId == space1.Id);
            apoios.Members.Should().HaveCount(1);
            apoios.Members.First().Name.Should().Be("João Silva");

            var cozinhas = res.Spaces.First(s => s.SpaceId == space2.Id);
            cozinhas.Members.Should().HaveCount(2);
            cozinhas.Members.First().Name.Should().Be("Pedro Souza");
            cozinhas.Members.Last().Name.Should().Be("Ana Lima");
        }
    }
}
