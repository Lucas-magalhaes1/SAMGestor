using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Service.Roster.Unassigned;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Application.Features.Service.Roster.Unassigned
{
    public class GetUnassignedServiceMembersHandlerTests
    {
        private readonly Mock<IRetreatRepository> _retreatRepo = new();
        private readonly Mock<IServiceRegistrationRepository> _regRepo = new();
        private readonly Mock<IServiceAssignmentRepository> _assignRepo = new();
        private readonly Mock<IServiceSpaceRepository> _spaceRepo = new();

        private GetUnassignedServiceMembersHandler Handler()
            => new GetUnassignedServiceMembersHandler(_retreatRepo.Object, _regRepo.Object, _assignRepo.Object, _spaceRepo.Object);

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

        private static ServiceRegistration NewReg(Guid retreatId, string name = "Fulano da Silva", string city = "SP")
            => new ServiceRegistration(
                retreatId,
                new FullName(name),
                new CPF("52998224725"),
                new EmailAddress($"{Guid.NewGuid()}@mail.com"),
                "11999999999",
                new DateOnly(1990, 1, 1),
                Gender.Male,
                city,
                "Oeste"
            );

        private static ServiceAssignment Assign(Guid spaceId, Guid regId)
            => new ServiceAssignment(spaceId, regId, ServiceRole.Member);

        [Fact]
        public async Task Throw_NotFound_when_retreat_missing()
        {
            var q = new GetUnassignedServiceMembersQuery(Guid.NewGuid());
            _retreatRepo.Setup(r => r.GetByIdAsync(q.RetreatId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Retreat?)null);

            await FluentActions.Invoking(() => Handler().Handle(q, default))
                .Should().ThrowAsync<NotFoundException>()
                .WithMessage("*Retreat*");
        }

        [Fact]
        public async Task Return_empty_when_no_unassigned_members()
        {
            var retreat = OpenRetreat();
            var cmd = new GetUnassignedServiceMembersQuery(retreat.Id);

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(retreat);
            _regRepo.Setup(r => r.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<ServiceRegistration>());
            _assignRepo.Setup(a => a.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new List<ServiceAssignment>());
            _spaceRepo.Setup(s => s.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<ServiceSpace>());

            var res = await Handler().Handle(cmd, default);

            res.Version.Should().Be(retreat.ServiceSpacesVersion);
            res.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task Return_filtered_unassigned_members()
        {
            var retreat = OpenRetreat();
            var cmd = new GetUnassignedServiceMembersQuery(retreat.Id, Gender: "Male", City: "SP");

            var reg1 = NewReg(retreat.Id, "João Silva", "SP");
            var reg2 = NewReg(retreat.Id, "Pedro Souza", "RJ");
            var reg3 = new ServiceRegistration(
                retreat.Id,
                new FullName("Ana Lima"),
                new CPF("52998224726"),
                new EmailAddress($"{Guid.NewGuid()}@mail.com"),
                "11999999998",
                new DateOnly(1990, 1, 1),
                Gender.Female,
                "SP",
                "Oeste"
            );

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(retreat);
            _regRepo.Setup(r => r.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<ServiceRegistration> { reg1, reg2, reg3 });
            _assignRepo.Setup(a => a.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new List<ServiceAssignment>());
            _spaceRepo.Setup(s => s.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<ServiceSpace>());

            var res = await Handler().Handle(cmd, default);

            res.Items.Should().HaveCount(1);
            res.Items.Single().Name.Should().Be("João Silva");
        }
    }
}
