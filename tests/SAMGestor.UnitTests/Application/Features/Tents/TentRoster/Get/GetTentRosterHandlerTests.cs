using System.Linq;
using System.Runtime.Serialization;
using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Tents.TentRoster.Assign;
using SAMGestor.Application.Features.Tents.TentRoster.Get;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Application.Features.Tents.TentRoster.Get;

public class GetTentRosterHandlerTests
{
    private static Retreat MakeRetreat(Guid id, int tentsVersion)
    {
        var r = (Retreat)FormatterServices.GetUninitializedObject(typeof(Retreat));
        typeof(Retreat).GetProperty("Id")!.SetValue(r, id);
        typeof(Retreat).GetProperty("TentsVersion")!.SetValue(r, tentsVersion);
        return r;
    }

    private static Tent MakeTent(Guid retreatId, TentCategory cat, int number, int capacity, bool isLocked = false)
    {
        var t = new Tent(new TentNumber(number), cat, capacity, retreatId);
        typeof(Tent).GetProperty("IsLocked")!.SetValue(t, isLocked);
        return t;
    }

    private static Registration MakeReg(Guid retreatId, string name, Gender gender, string? city = null)
    {
        var r = (Registration)FormatterServices.GetUninitializedObject(typeof(Registration));
        typeof(Registration).GetProperty("Id")!.SetValue(r, Guid.NewGuid());
        typeof(Registration).GetProperty("RetreatId")!.SetValue(r, retreatId);

        var safeName = string.IsNullOrWhiteSpace(name) || !name.Contains(' ')
            ? $"{name} Silva"
            : name;
        typeof(Registration).GetProperty("Name")!.SetValue(r, new FullName(safeName));

        typeof(Registration).GetProperty("Gender")!.SetValue(r, gender);
        typeof(Registration).GetProperty("City")!.SetValue(r, city);
        return r;
    }


    private static TentAssignment MakeAssign(Guid tentId, Guid regId, int? position, DateTime assignedAt)
    {
        var a = new TentAssignment(tentId, regId, position);
        var dto = assignedAt.Kind == DateTimeKind.Unspecified
            ? new DateTimeOffset(DateTime.SpecifyKind(assignedAt, DateTimeKind.Utc))
            : new DateTimeOffset(assignedAt.ToUniversalTime());
        typeof(TentAssignment).GetProperty("AssignedAt")!.SetValue(a, dto);
        return a;
    }


    [Fact]
    public async Task Should_throw_when_retreat_not_found()
    {
        var retId = Guid.NewGuid();

        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync((Retreat?)null);

        var tentRepo = new Mock<ITentRepository>();
        var assignRepo = new Mock<ITentAssignmentRepository>();
        var regRepo = new Mock<IRegistrationRepository>();

        var handler = new GetTentRosterHandler(retreatRepo.Object, tentRepo.Object, assignRepo.Object, regRepo.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetTentRosterQuery(retId), CancellationToken.None));
    }

    [Fact]
    public async Task No_tents_should_return_version_and_empty_list()
    {
        var retId = Guid.NewGuid();
        var retreat = MakeRetreat(retId, tentsVersion: 7);

        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(retreat);

        var tentRepo = new Mock<ITentRepository>();
        tentRepo.Setup(t => t.ListByRetreatAsync(retId, null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Tent>());

        var assignRepo = new Mock<ITentAssignmentRepository>();
        var regRepo = new Mock<IRegistrationRepository>();

        var handler = new GetTentRosterHandler(retreatRepo.Object, tentRepo.Object, assignRepo.Object, regRepo.Object);

        var res = await handler.Handle(new GetTentRosterQuery(retId), CancellationToken.None);

        res.Version.Should().Be(7);
        res.Tents.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_build_roster_grouped_and_sorted_with_position_coalesce()
    {
        var retId = Guid.NewGuid();
        var retreat = MakeRetreat(retId, tentsVersion: 3);

        var t1 = MakeTent(retId, TentCategory.Male,   1, 4, isLocked: false);
        var t2 = MakeTent(retId, TentCategory.Female, 2, 4, isLocked: true);

        var rA = MakeReg(retId, "Alpha", Gender.Male,   "SP");
        var rB = MakeReg(retId, "Bravo", Gender.Male,   "SP");
        var rC = MakeReg(retId, "Charlie", Gender.Female, "RJ");

        var a1 = MakeAssign(t1.Id, rA.Id, position: 1, assignedAt: new DateTime(2024, 1, 2));
        var a2 = MakeAssign(t1.Id, rB.Id, position: null, assignedAt: new DateTime(2024, 1, 1));
        var a3 = MakeAssign(t2.Id, rC.Id, position: 0, assignedAt: new DateTime(2024, 1, 3));

        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(retreat);

        var tentRepo = new Mock<ITentRepository>();
        tentRepo.Setup(t => t.ListByRetreatAsync(retId, null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Tent> { t1, t2 });

        var assignRepo = new Mock<ITentAssignmentRepository>();
        assignRepo.Setup(a => a.ListByTentIdsAsync(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<TentAssignment> { a1, a2, a3 });

        var regRepo = new Mock<IRegistrationRepository>();
        regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration>
               {
                   [rA.Id] = rA, [rB.Id] = rB, [rC.Id] = rC
               });

        var handler = new GetTentRosterHandler(retreatRepo.Object, tentRepo.Object, assignRepo.Object, regRepo.Object);

        var res = await handler.Handle(new GetTentRosterQuery(retId), CancellationToken.None);

        res.Version.Should().Be(3);
        res.Tents.Should().HaveCount(2);

        var v1 = res.Tents.Single(x => x.TentId == t1.Id);
        v1.Number.Should().Be("1");
        v1.Category.Should().Be(TentCategory.Male.ToString());
        v1.IsLocked.Should().BeFalse();
        v1.Members.Should().HaveCount(2);
        v1.Members.Select(m => m.RegistrationId).Should().BeEquivalentTo(new[] { rA.Id, rB.Id }, o => o.WithStrictOrdering());
        v1.Members.Select(m => m.Position).Should().BeEquivalentTo(new[] { 1, 1 }, o => o.WithStrictOrdering());

        var v2 = res.Tents.Single(x => x.TentId == t2.Id);
        v2.Number.Should().Be("2");
        v2.Category.Should().Be(TentCategory.Female.ToString());
        v2.IsLocked.Should().BeTrue();
        v2.Members.Should().HaveCount(1);
        v2.Members[0].RegistrationId.Should().Be(rC.Id);
        v2.Members[0].Position.Should().Be(0);
    }

    [Fact]
    public async Task Should_ignore_assignments_with_missing_registration_in_map()
    {
        var retId = Guid.NewGuid();
        var retreat = MakeRetreat(retId, 9);

        var t = MakeTent(retId, TentCategory.Male, 10, 2);

        var rOk = MakeReg(retId, "Presente", Gender.Male, "BH");
        var ghostId = Guid.NewGuid();

        var aOk    = MakeAssign(t.Id, rOk.Id,    0, new DateTime(2024, 5, 1));
        var aGhost = MakeAssign(t.Id, ghostId,   1, new DateTime(2024, 5, 2));

        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(retreat);

        var tentRepo = new Mock<ITentRepository>();
        tentRepo.Setup(t => t.ListByRetreatAsync(retId, null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Tent> { t });

        var assignRepo = new Mock<ITentAssignmentRepository>();
        assignRepo.Setup(a => a.ListByTentIdsAsync(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<TentAssignment> { aOk, aGhost });

        var regRepo = new Mock<IRegistrationRepository>();
        regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> { [rOk.Id] = rOk });

        var handler = new GetTentRosterHandler(retreatRepo.Object, tentRepo.Object, assignRepo.Object, regRepo.Object);

        var res = await handler.Handle(new GetTentRosterQuery(retId), CancellationToken.None);

        res.Version.Should().Be(9);
        res.Tents.Should().HaveCount(1);
        var v = res.Tents[0];
        v.Members.Should().HaveCount(1);
        v.Members[0].RegistrationId.Should().Be(rOk.Id);
        v.Members[0].Position.Should().Be(0);
    }
}
