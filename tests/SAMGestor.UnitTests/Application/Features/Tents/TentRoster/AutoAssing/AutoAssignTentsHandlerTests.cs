using System.Linq;
using System.Runtime.Serialization;
using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Tents.TentRoster.AutoAssign;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Application.Features.Tents.TentRoster.AutoAssing;

public class AutoAssignTentsHandlerTests
{
    private static Retreat MakeRetreat(Guid id, int tentsVersion, bool tentsLocked)
    {
        var r = (Retreat)FormatterServices.GetUninitializedObject(typeof(Retreat));
        typeof(Retreat).GetProperty("Id")!.SetValue(r, id);
        typeof(Retreat).GetProperty("TentsVersion", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!.SetValue(r, tentsVersion);
        typeof(Retreat).GetProperty("TentsLocked",  System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)!.SetValue(r, tentsLocked);
        return r;
    }

    private static Tent MakeTent(Guid retreatId, TentCategory cat, int number, int capacity, bool isLocked = false)
    {
        var t = new Tent(new TentNumber(number), cat, capacity, retreatId);
        typeof(Tent).GetProperty("IsLocked")!.SetValue(t, isLocked);
        return t;
    }

    private static Registration MakeReg(Guid retreatId, string name, Gender gender, bool enabled, RegistrationStatus status, string? city = null)
    {
        var r = (Registration)FormatterServices.GetUninitializedObject(typeof(Registration));
        typeof(Registration).GetProperty("Id")!.SetValue(r, Guid.NewGuid());
        typeof(Registration).GetProperty("RetreatId")!.SetValue(r, retreatId);
        typeof(Registration).GetProperty("Name")!.SetValue(r, new FullName(name));
        typeof(Registration).GetProperty("Gender")!.SetValue(r, gender);
        typeof(Registration).GetProperty("Enabled")!.SetValue(r, enabled);
        typeof(Registration).GetProperty("Status")!.SetValue(r, status);
        typeof(Registration).GetProperty("City")!.SetValue(r, city);
        return r;
    }

    [Fact]
    public async Task Should_throw_when_retreat_not_found()
    {
        var retId = Guid.NewGuid();

        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retId, It.IsAny<CancellationToken>())).ReturnsAsync((Retreat?)null);

        var tentRepo = new Mock<ITentRepository>();
        var assignRepo = new Mock<ITentAssignmentRepository>();
        var regRepo = new Mock<IRegistrationRepository>();
        var uow = new Mock<IUnitOfWork>();

        var handler = new AutoAssignTentsHandler(retreatRepo.Object, tentRepo.Object, assignRepo.Object, regRepo.Object, uow.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new AutoAssignTentsCommand(retId), CancellationToken.None));
    }

    [Fact]
    public async Task Should_throw_when_retreat_locked()
    {
        var retId = Guid.NewGuid();
        var retreat = MakeRetreat(retId, 3, true);

        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retId, It.IsAny<CancellationToken>())).ReturnsAsync(retreat);

        var tentRepo = new Mock<ITentRepository>();
        var assignRepo = new Mock<ITentAssignmentRepository>();
        var regRepo = new Mock<IRegistrationRepository>();
        var uow = new Mock<IUnitOfWork>();

        var handler = new AutoAssignTentsHandler(retreatRepo.Object, tentRepo.Object, assignRepo.Object, regRepo.Object, uow.Object);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.Handle(new AutoAssignTentsCommand(retId), CancellationToken.None));
    }

    [Fact]
    public async Task No_tents_should_return_empty_response()
    {
        var retId = Guid.NewGuid();
        var retreat = MakeRetreat(retId, 5, false);

        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retId, It.IsAny<CancellationToken>())).ReturnsAsync(retreat);

        var tentRepo = new Mock<ITentRepository>();
        tentRepo.Setup(t => t.ListByRetreatAsync(retId, null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Tent>());

        var assignRepo = new Mock<ITentAssignmentRepository>();
        var regRepo = new Mock<IRegistrationRepository>();
        var uow = new Mock<IUnitOfWork>();

        var handler = new AutoAssignTentsHandler(retreatRepo.Object, tentRepo.Object, assignRepo.Object, regRepo.Object, uow.Object);

        var res = await handler.Handle(new AutoAssignTentsCommand(retId), CancellationToken.None);

        res.Version.Should().Be(5);
        res.Tents.Should().BeEmpty();
        assignRepo.Verify(a => a.AddRangeAsync(It.IsAny<IEnumerable<TentAssignment>>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RespectLocked_true_should_ignore_locked_tents()
    {
        var retId = Guid.NewGuid();
        var retreat = MakeRetreat(retId, 1, false);

        var tUnlocked = MakeTent(retId, TentCategory.Male, 1, 2, isLocked: false);
        var tLocked   = MakeTent(retId, TentCategory.Male, 2, 2, isLocked: true);

        var r1 = MakeReg(retId, "Joao Silva", Gender.Male, true, RegistrationStatus.PaymentConfirmed);
        var r2 = MakeReg(retId, "Pedro Lima", Gender.Male, true, RegistrationStatus.Confirmed);

        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retId, It.IsAny<CancellationToken>())).ReturnsAsync(retreat);
        retreatRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var tentRepo = new Mock<ITentRepository>();
        tentRepo.Setup(t => t.ListByRetreatAsync(retId, null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Tent> { tUnlocked, tLocked });

        var added = new List<TentAssignment>();
        var assignRepo = new Mock<ITentAssignmentRepository>();
        assignRepo.SetupSequence(a => a.ListByTentIdsAsync(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<TentAssignment>())
                  .ReturnsAsync(() => added);
        assignRepo.Setup(a => a.AddRangeAsync(It.IsAny<IEnumerable<TentAssignment>>(), It.IsAny<CancellationToken>()))
                  .Callback<IEnumerable<TentAssignment>, CancellationToken>((l, _) => added = l.ToList())
                  .Returns(Task.CompletedTask);

        var regRepo = new Mock<IRegistrationRepository>();
        regRepo.Setup(r => r.ListPaidUnassignedAsync(retId, Gender.Male,  null, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<Registration> { r1, r2 });
        regRepo.Setup(r => r.ListPaidUnassignedAsync(retId, Gender.Female, null, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<Registration>());
        regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> { [r1.Id] = r1, [r2.Id] = r2 });

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(1));

        var handler = new AutoAssignTentsHandler(retreatRepo.Object, tentRepo.Object, assignRepo.Object, regRepo.Object, uow.Object);

        var res = await handler.Handle(new AutoAssignTentsCommand(retId, RespectLocked: true), CancellationToken.None);

        added.Should().OnlyContain(a => a.TentId == tUnlocked.Id);
        res.Tents.Single(x => x.TentId == tUnlocked.Id).Members.Should().HaveCount(2);
        res.Tents.Single(x => x.TentId == tLocked.Id).Members.Should().BeEmpty();
    }

    [Fact]
    public async Task RespectLocked_false_should_fill_locked_tents_too()
    {
        var retId = Guid.NewGuid();
        var retreat = MakeRetreat(retId, 2, false);

        var tLocked = MakeTent(retId, TentCategory.Female, 1, 3, isLocked: true);

        var r1 = MakeReg(retId, "Ana Souza", Gender.Female, true, RegistrationStatus.PaymentConfirmed);
        var r2 = MakeReg(retId, "Bianca Dias", Gender.Female, true, RegistrationStatus.Confirmed);

        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retId, It.IsAny<CancellationToken>())).ReturnsAsync(retreat);
        retreatRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var tentRepo = new Mock<ITentRepository>();
        tentRepo.Setup(t => t.ListByRetreatAsync(retId, null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Tent> { tLocked });

        var added = new List<TentAssignment>();
        var assignRepo = new Mock<ITentAssignmentRepository>();
        assignRepo.SetupSequence(a => a.ListByTentIdsAsync(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<TentAssignment>())
                  .ReturnsAsync(() => added);
        assignRepo.Setup(a => a.AddRangeAsync(It.IsAny<IEnumerable<TentAssignment>>(), It.IsAny<CancellationToken>()))
                  .Callback<IEnumerable<TentAssignment>, CancellationToken>((l, _) => added = l.ToList())
                  .Returns(Task.CompletedTask);

        var regRepo = new Mock<IRegistrationRepository>();
        regRepo.Setup(r => r.ListPaidUnassignedAsync(retId, Gender.Male,   null, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<Registration>());
        regRepo.Setup(r => r.ListPaidUnassignedAsync(retId, Gender.Female, null, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<Registration> { r1, r2 });
        regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> { [r1.Id] = r1, [r2.Id] = r2 });

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(1));

        var handler = new AutoAssignTentsHandler(retreatRepo.Object, tentRepo.Object, assignRepo.Object, regRepo.Object, uow.Object);

        var res = await handler.Handle(new AutoAssignTentsCommand(retId, RespectLocked: false), CancellationToken.None);

        added.Should().OnlyContain(a => a.TentId == tLocked.Id);
        res.Tents.Single(x => x.TentId == tLocked.Id).Members.Should().HaveCount(2);
    }

    [Fact]
    public async Task Should_respect_existing_occupancy_and_append_positions()
    {
        var retId = Guid.NewGuid();
        var retreat = MakeRetreat(retId, 10, false);

        var tA = MakeTent(retId, TentCategory.Male, 1, 3);
        var existingReg = MakeReg(retId, "Usuario Existente", Gender.Male, true, RegistrationStatus.Confirmed);

        var r1 = MakeReg(retId, "Novo Um", Gender.Male, true, RegistrationStatus.PaymentConfirmed);
        var r2 = MakeReg(retId, "Novo Dois", Gender.Male, true, RegistrationStatus.PaymentConfirmed);

        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retId, It.IsAny<CancellationToken>())).ReturnsAsync(retreat);
        retreatRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var tentRepo = new Mock<ITentRepository>();
        tentRepo.Setup(t => t.ListByRetreatAsync(retId, null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Tent> { tA });

        var added = new List<TentAssignment>();
        var assignRepo = new Mock<ITentAssignmentRepository>();
        assignRepo.SetupSequence(a => a.ListByTentIdsAsync(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<TentAssignment> { new TentAssignment(tA.Id, existingReg.Id, 0) })
                  .ReturnsAsync(() => new List<TentAssignment>(new []
                  {
                      new TentAssignment(tA.Id, existingReg.Id, 0),
                      new TentAssignment(tA.Id, r1.Id, 1),
                      new TentAssignment(tA.Id, r2.Id, 2)
                  }));
        assignRepo.Setup(a => a.AddRangeAsync(It.IsAny<IEnumerable<TentAssignment>>(), It.IsAny<CancellationToken>()))
                  .Callback<IEnumerable<TentAssignment>, CancellationToken>((l, _) => added = l.ToList())
                  .Returns(Task.CompletedTask);

        var regRepo = new Mock<IRegistrationRepository>();
        regRepo.Setup(r => r.ListPaidUnassignedAsync(retId, Gender.Male, null, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<Registration> { r1, r2 });
        regRepo.Setup(r => r.ListPaidUnassignedAsync(retId, Gender.Female, null, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<Registration>());
        regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> { [existingReg.Id]=existingReg, [r1.Id]=r1, [r2.Id]=r2 });

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(1));

        var handler = new AutoAssignTentsHandler(retreatRepo.Object, tentRepo.Object, assignRepo.Object, regRepo.Object, uow.Object);

        var res = await handler.Handle(new AutoAssignTentsCommand(retId), CancellationToken.None);

        added.Should().HaveCount(2);
        added.Select(a => a.Position).Should().BeEquivalentTo(new[] { 1, 2 }, opt => opt.WithStrictOrdering());
        var view = res.Tents.Single(t => t.TentId == tA.Id);
        view.Members.Should().HaveCount(3);
        view.Members.Select(m => m.Position).Should().BeEquivalentTo(new[] { 0, 1, 2 }, opt => opt.WithStrictOrdering());
    }

    [Fact]
    public async Task Should_distribute_across_same_category_by_lowest_occupancy_then_number()
    {
        var retId = Guid.NewGuid();
        var retreat = MakeRetreat(retId, 20, false);

        var t1 = MakeTent(retId, TentCategory.Female, 1, 2);
        var t2 = MakeTent(retreat.Id, TentCategory.Female, 2, 2);

        var f1 = MakeReg(retId, "F Um", Gender.Female, true, RegistrationStatus.PaymentConfirmed);
        var f2 = MakeReg(retId, "F Dois", Gender.Female, true, RegistrationStatus.PaymentConfirmed);

        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retId, It.IsAny<CancellationToken>())).ReturnsAsync(retreat);
        retreatRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var tentRepo = new Mock<ITentRepository>();
        tentRepo.Setup(t => t.ListByRetreatAsync(retId, null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Tent> { t1, t2 });

        var added = new List<TentAssignment>();
        var assignRepo = new Mock<ITentAssignmentRepository>();
        assignRepo.SetupSequence(a => a.ListByTentIdsAsync(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<TentAssignment>())
                  .ReturnsAsync(() => added);
        assignRepo.Setup(a => a.AddRangeAsync(It.IsAny<IEnumerable<TentAssignment>>(), It.IsAny<CancellationToken>()))
                  .Callback<IEnumerable<TentAssignment>, CancellationToken>((l, _) => added = l.ToList())
                  .Returns(Task.CompletedTask);

        var regRepo = new Mock<IRegistrationRepository>();
        regRepo.Setup(r => r.ListPaidUnassignedAsync(retId, Gender.Male,   null, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<Registration>());
        regRepo.Setup(r => r.ListPaidUnassignedAsync(retId, Gender.Female, null, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<Registration> { f1, f2 });
        regRepo.Setup(r => r.GetMapByIdsAsync(It.IsAny<Guid[]>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> { [f1.Id]=f1, [f2.Id]=f2 });

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(1));

        var handler = new AutoAssignTentsHandler(retreatRepo.Object, tentRepo.Object, assignRepo.Object, regRepo.Object, uow.Object);

        var res = await handler.Handle(new AutoAssignTentsCommand(retId), CancellationToken.None);

        added.Should().HaveCount(2);
        added.Select(a => a.TentId).Distinct().Should().BeEquivalentTo(new[] { t1.Id, t2.Id });
        var view1 = res.Tents.Single(t => t.TentId == t1.Id);
        var view2 = res.Tents.Single(t => t.TentId == t2.Id);
        view1.Members.Should().HaveCount(1);
        view2.Members.Should().HaveCount(1);
    }
}
