using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Service.Alerts.GetAll;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Application.Features.Service.Alerts.GetAll
{
    public class GetServiceAlertsHandlerTests
    {
        private readonly Mock<IRetreatRepository>              _retreatRepo = new();
        private readonly Mock<IServiceSpaceRepository>         _spaceRepo   = new();
        private readonly Mock<IServiceAssignmentRepository>    _assignRepo  = new();
        private readonly Mock<IServiceRegistrationRepository>  _regRepo     = new();

        private GetServiceAlertsHandler Handler()
            => new GetServiceAlertsHandler(_retreatRepo.Object, _spaceRepo.Object, _assignRepo.Object, _regRepo.Object);

        private static Retreat OpenRetreat()
            => new Retreat(
                new FullName("Retiro Service"),
                "ED1", "Tema",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12)),
                10, 10,
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
                new Money(0,"BRL"), new Money(0,"BRL"),
                new Percentage(50), new Percentage(50));

        private static ServiceSpace Space(Guid retreatId, string name, int min, int max)
            => new ServiceSpace(retreatId, name, description: null, maxPeople: max, minPeople: min);

        private static ServiceAssignment Assign(Guid spaceId, ServiceRole role)
            => new ServiceAssignment(spaceId, Guid.NewGuid(), role);

        [Fact]
        public async Task Throw_NotFound_when_retreat_missing()
        {
            var q = new GetServiceAlertsQuery(Guid.NewGuid(), ServiceAlertMode.All);

            _retreatRepo.Setup(r => r.GetByIdAsync(q.RetreatId, It.IsAny<CancellationToken>()))
                        .ReturnsAsync((Retreat?)null);

            var act = () => Handler().Handle(q, default);

            await act.Should().ThrowAsync<NotFoundException>()
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

            var res = await Handler().Handle(new GetServiceAlertsQuery(retreat.Id, ServiceAlertMode.All), default);

            res.Version.Should().Be(retreat.ServiceSpacesVersion);
            res.GeneratedAtUtc.Should().BeAfter(DateTime.UtcNow.AddMinutes(-5));
            res.Spaces.Should().BeEmpty();

            _assignRepo.Verify(a => a.ListByRetreatAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _regRepo.Verify(r => r.CountPreferencesBySpaceAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Preferences_alerts_are_generated_and_filtered()
        {
            var retreat = OpenRetreat();

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(retreat);

            var s1 = Space(retreat.Id, "Apoio",       min: 1, max: 3);
            var s2 = Space(retreat.Id, "Cozinha",     min: 3, max: 5);
            var s3 = Space(retreat.Id, "Manutenção",  min: 1, max: 2);
            var sOK= Space(retreat.Id, "Capela",      min: 2, max: 4);

            _spaceRepo.Setup(s => s.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<ServiceSpace> { s1, s2, s3, sOK });

            _assignRepo.Setup(a => a.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new List<ServiceAssignment>());

            _regRepo.Setup(r => r.CountPreferencesBySpaceAsync(retreat.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Dictionary<Guid,int>
                    {
                        [s2.Id] = 2,
                        [s3.Id] = 4,
                        [sOK.Id]= 3
                    });

            var q = new GetServiceAlertsQuery(retreat.Id, ServiceAlertMode.Preferences);
            var res = await Handler().Handle(q, default);

            res.Spaces.Select(x => x.SpaceId).Should().BeEquivalentTo(new[] { s1.Id, s2.Id, s3.Id });

            var s1Alerts = res.Spaces.First(x => x.SpaceId == s1.Id).Alerts;
            s1Alerts.Should().ContainSingle(a => a.Code == "NoPreferences" && a.Severity == "info");

            var s2Alerts = res.Spaces.First(x => x.SpaceId == s2.Id).Alerts;
            s2Alerts.Should().ContainSingle(a => a.Code == "PreferenceBelowMin" && a.Severity == "warning");

            var s3Alerts = res.Spaces.First(x => x.SpaceId == s3.Id).Alerts;
            s3Alerts.Should().ContainSingle(a => a.Code == "PreferenceOverMax" && a.Severity == "warning");

            var allowed = new[] { "NoPreferences", "PreferenceBelowMin", "PreferenceOverMax" };
            res.Spaces.Should().OnlyContain(sp => sp.Alerts.All(a => allowed.Contains(a.Code)));
        }

        [Fact]
        public async Task Roster_alerts_are_generated_and_filtered_and_spaces_without_alerts_are_omitted()
        {
            var retreat = OpenRetreat();

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(retreat);

            var sR1 = Space(retreat.Id, "Tapera",      min: 1, max: 3);
            var sR2 = Space(retreat.Id, "Externa",     min: 2, max: 5);
            var sR3 = Space(retreat.Id, "Loja",        min: 1, max: 1);
            var sOK = Space(retreat.Id, "Secretaria",  min: 2, max: 4);

            _spaceRepo.Setup(s => s.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<ServiceSpace> { sR1, sR2, sR3, sOK });

            _regRepo.Setup(r => r.CountPreferencesBySpaceAsync(retreat.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Dictionary<Guid,int>());

            _assignRepo.Setup(a => a.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new List<ServiceAssignment>
                       {
                           Assign(sR2.Id, ServiceRole.Vice),
                           Assign(sR3.Id, ServiceRole.Coordinator),
                           Assign(sR3.Id, ServiceRole.Member),
                           Assign(sOK.Id, ServiceRole.Coordinator),
                           Assign(sOK.Id, ServiceRole.Vice),
                       });

            var q = new GetServiceAlertsQuery(retreat.Id, ServiceAlertMode.Roster);
            var res = await Handler().Handle(q, default);

            res.Spaces.Select(x => x.SpaceId).Should().BeEquivalentTo(new[] { sR1.Id, sR2.Id, sR3.Id });

            var r1 = res.Spaces.First(x => x.SpaceId == sR1.Id);
            r1.AssignedCount.Should().Be(0);
            r1.Alerts.Should().Contain(a => a.Code == "MissingCoordinator");
            r1.Alerts.Should().Contain(a => a.Code == "MissingVice");
            r1.Alerts.Should().Contain(a => a.Code == "BelowMin");
            r1.Alerts.Should().NotContain(a => a.Code == "OverMax");

            var r2 = res.Spaces.First(x => x.SpaceId == sR2.Id);
            r2.AssignedCount.Should().Be(1);
            r2.Alerts.Should().Contain(a => a.Code == "MissingCoordinator");
            r2.Alerts.Should().Contain(a => a.Code == "BelowMin");
            r2.Alerts.Should().NotContain(a => a.Code == "MissingVice");
            r2.Alerts.Should().NotContain(a => a.Code == "OverMax");

            var r3 = res.Spaces.First(x => x.SpaceId == sR3.Id);
            r3.AssignedCount.Should().Be(2);
            r3.Alerts.Should().Contain(a => a.Code == "MissingVice");
            r3.Alerts.Should().Contain(a => a.Code == "OverMax");
            r3.Alerts.Should().NotContain(a => a.Code == "MissingCoordinator");
            r3.Alerts.Should().NotContain(a => a.Code == "BelowMin");
        }

        [Fact]
        public async Task All_mode_includes_both_preferences_and_roster_alerts()
        {
            var retreat = OpenRetreat();

            _retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(retreat);

            var s = Space(retreat.Id, "Música", min: 2, max: 2);

            _spaceRepo.Setup(sx => sx.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
                      .ReturnsAsync(new List<ServiceSpace> { s });

            _regRepo.Setup(r => r.CountPreferencesBySpaceAsync(retreat.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new Dictionary<Guid,int>());

            _assignRepo.Setup(a => a.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(new List<ServiceAssignment>
                       {
                           Assign(s.Id, ServiceRole.Member)
                       });

            var res = await Handler().Handle(new GetServiceAlertsQuery(retreat.Id, ServiceAlertMode.All), default);

            res.Spaces.Should().ContainSingle(x => x.SpaceId == s.Id);
            var alerts = res.Spaces.Single().Alerts.Select(a => a.Code).ToList();

            alerts.Should().Contain("NoPreferences");
            alerts.Should().Contain("MissingCoordinator");
            alerts.Should().Contain("MissingVice");
            alerts.Should().Contain("BelowMin");
            alerts.Should().NotContain("OverMax");
        }
    }
}
