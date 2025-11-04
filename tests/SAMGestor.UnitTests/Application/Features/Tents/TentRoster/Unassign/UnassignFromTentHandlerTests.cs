using System.Runtime.Serialization;
using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Tents.TentRoster.Unassign;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;
using SAMGestor.Domain.Enums;
using SAMGestor.UnitTests.Dependencies;

namespace SAMGestor.UnitTests.Application.Features.Tents.TentRoster.Unassign;

public class UnassignFromTentHandlerTests
{
    private static Retreat MakeRetreat(Guid id, int tentsVersion, bool tentsLocked)
    {
        var r = TestObjectFactory.Uninitialized<Retreat>();
        typeof(Retreat).GetProperty("Id")!.SetValue(r, id);
        typeof(Retreat).GetProperty("TentsVersion")!.SetValue(r, tentsVersion);
        typeof(Retreat).GetProperty("TentsLocked")!.SetValue(r, tentsLocked);
        return r;
    }

    private static Tent MakeTent(Guid retreatId, TentCategory cat, int number, int capacity, bool isLocked = false)
    {
        var t = new Tent(new TentNumber(number), cat, capacity, retreatId);
        typeof(Tent).GetProperty("IsLocked")!.SetValue(t, isLocked);
        return t;
    }

    private static TentAssignment MakeAssign(Guid tentId, Guid regId, int? position = null)
    {
        return new TentAssignment(tentId, regId, position);
    }

    [Fact]
    public async Task Should_throw_when_retreat_not_found()
    {
        var retId = Guid.NewGuid();
        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retId, It.IsAny<CancellationToken>())).ReturnsAsync((Retreat?)null);

        var tentRepo = new Mock<ITentRepository>();
        var assignRepo = new Mock<ITentAssignmentRepository>();
        var uow = new Mock<IUnitOfWork>();

        var handler = new UnassignFromTentHandler(retreatRepo.Object, tentRepo.Object, assignRepo.Object, uow.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UnassignFromTentCommand(retId, new[] { Guid.NewGuid() }), CancellationToken.None));
    }

    [Fact]
    public async Task Should_throw_when_retreat_locked()
    {
        var retId = Guid.NewGuid();
        var retreat = MakeRetreat(retId, 5, true);

        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retId, It.IsAny<CancellationToken>())).ReturnsAsync(retreat);

        var tentRepo = new Mock<ITentRepository>();
        var assignRepo = new Mock<ITentAssignmentRepository>();
        var uow = new Mock<IUnitOfWork>();

        var handler = new UnassignFromTentHandler(retreatRepo.Object, tentRepo.Object, assignRepo.Object, uow.Object);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.Handle(new UnassignFromTentCommand(retId, new[] { Guid.NewGuid() }), CancellationToken.None));
    }

    [Fact]
    public async Task No_assignments_should_return_zero_and_not_touch_version()
    {
        var retId = Guid.NewGuid();
        var retreat = MakeRetreat(retId, 7, false);

        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retId, It.IsAny<CancellationToken>())).ReturnsAsync(retreat);

        var tentRepo = new Mock<ITentRepository>();
        var assignRepo = new Mock<ITentAssignmentRepository>();
        assignRepo.Setup(a => a.GetByRegistrationIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((TentAssignment?)null);

        var uow = new Mock<IUnitOfWork>();

        var handler = new UnassignFromTentHandler(retreatRepo.Object, tentRepo.Object, assignRepo.Object, uow.Object);

        var res = await handler.Handle(new UnassignFromTentCommand(retId, new[] { Guid.NewGuid(), Guid.NewGuid() }), CancellationToken.None);

        res.Version.Should().Be(7);
        res.Removed.Should().Be(0);
        res.AffectedTentIds.Should().BeEmpty();
        assignRepo.Verify(a => a.RemoveRangeAsync(It.IsAny<IEnumerable<TentAssignment>>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Should_unassign_and_bump_version()
    {
        var retId = Guid.NewGuid();
        var retreat = MakeRetreat(retId, 3, false);

        var t1 = MakeTent(retId, TentCategory.Male, 1, 4);
        var t2 = MakeTent(retId, TentCategory.Female, 2, 4);

        var r1 = Guid.NewGuid();
        var r2 = Guid.NewGuid();

        var a1 = MakeAssign(t1.Id, r1, 0);
        var a2 = MakeAssign(t2.Id, r2, 0);

        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retId, It.IsAny<CancellationToken>())).ReturnsAsync(retreat);
        retreatRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var tentRepo = new Mock<ITentRepository>();
        tentRepo.Setup(t => t.ListByRetreatAsync(retId, null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Tent> { t1, t2 });

        var assignRepo = new Mock<ITentAssignmentRepository>();
        assignRepo.Setup(a => a.GetByRegistrationIdAsync(r1, It.IsAny<CancellationToken>())).ReturnsAsync(a1);
        assignRepo.Setup(a => a.GetByRegistrationIdAsync(r2, It.IsAny<CancellationToken>())).ReturnsAsync(a2);
        assignRepo.Setup(a => a.RemoveRangeAsync(It.IsAny<IEnumerable<TentAssignment>>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(1));

        var handler = new UnassignFromTentHandler(retreatRepo.Object, tentRepo.Object, assignRepo.Object, uow.Object);

        var res = await handler.Handle(new UnassignFromTentCommand(retId, new[] { r1, r2 }), CancellationToken.None);

        assignRepo.Verify(a => a.RemoveRangeAsync(It.Is<IEnumerable<TentAssignment>>(l => l.Count() == 2), It.IsAny<CancellationToken>()), Times.Once);
        res.Removed.Should().Be(2);
        res.AffectedTentIds.Should().BeEquivalentTo(new[] { t1.Id, t2.Id });
        res.Version.Should().Be(4);
    }

    [Fact]
    public async Task Should_throw_when_any_impacted_tent_is_locked()
    {
        var retId = Guid.NewGuid();
        var retreat = MakeRetreat(retId, 9, false);

        var tLocked = MakeTent(retId, TentCategory.Male, 10, 2, isLocked: true);
        var tOpen   = MakeTent(retId, TentCategory.Male, 11, 2, isLocked: false);

        var r1 = Guid.NewGuid();
        var r2 = Guid.NewGuid();

        var a1 = MakeAssign(tLocked.Id, r1, 0);
        var a2 = MakeAssign(tOpen.Id,   r2, 0);

        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retId, It.IsAny<CancellationToken>())).ReturnsAsync(retreat);

        var tentRepo = new Mock<ITentRepository>();
        tentRepo.Setup(t => t.ListByRetreatAsync(retId, null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Tent> { tLocked, tOpen });

        var assignRepo = new Mock<ITentAssignmentRepository>();
        assignRepo.Setup(a => a.GetByRegistrationIdAsync(r1, It.IsAny<CancellationToken>())).ReturnsAsync(a1);
        assignRepo.Setup(a => a.GetByRegistrationIdAsync(r2, It.IsAny<CancellationToken>())).ReturnsAsync(a2);

        var uow = new Mock<IUnitOfWork>();

        var handler = new UnassignFromTentHandler(retreatRepo.Object, tentRepo.Object, assignRepo.Object, uow.Object);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.Handle(new UnassignFromTentCommand(retId, new[] { r1, r2 }), CancellationToken.None));

        assignRepo.Verify(a => a.RemoveRangeAsync(It.IsAny<IEnumerable<TentAssignment>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
