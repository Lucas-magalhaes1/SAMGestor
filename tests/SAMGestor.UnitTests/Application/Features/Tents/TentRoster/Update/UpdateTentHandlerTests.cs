using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Tents.Update;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;
using SAMGestor.UnitTests.Dependencies;

namespace SAMGestor.UnitTests.Application.Features.Tents.TentRoster.Update;

public class UpdateTentHandlerTests
{
    private static Retreat MakeRetreat(Guid id, int tentsVersion, bool tentsLocked)
    {
        var r = TestObjectFactory.Uninitialized<Retreat>();
        typeof(Retreat).GetProperty("Id")!.SetValue(r, id);
        typeof(Retreat).GetProperty("TentsVersion")!.SetValue(r, tentsVersion);
        typeof(Retreat).GetProperty("TentsLocked")!.SetValue(r, tentsLocked);
        return r;
    }

    private static Tent MakeTent(Guid retreatId, TentCategory cat, int number, int capacity, bool isLocked = false, bool isActive = true, string? notes = null)
    {
        var t = new Tent(new TentNumber(number), cat, capacity, retreatId);
        typeof(Tent).GetProperty("IsLocked")!.SetValue(t, isLocked);
        typeof(Tent).GetProperty("IsActive")!.SetValue(t, isActive);
        if (notes is not null) typeof(Tent).GetProperty("Notes")!.SetValue(t, notes);
        return t;
    }

    private static UpdateTentHandler BuildHandler(
        Retreat retreat,
        Tent? tent,
        int assigned = 0,
        bool existsNumber = false)
    {
        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>())).ReturnsAsync(retreat);
        retreatRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var tentRepo = new Mock<ITentRepository>();
        tentRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tent);
        tentRepo.Setup(r => r.ExistsNumberAsync(
                It.IsAny<Guid>(),
                It.IsAny<TentCategory>(),
                It.IsAny<TentNumber>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(existsNumber);
        tentRepo.Setup(r => r.UpdateAsync(It.IsAny<Tent>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        var regRepo = new Mock<IRegistrationRepository>();
        regRepo.Setup(r => r.CountByTentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(assigned));

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Returns(Task.FromResult(1));

        return new UpdateTentHandler(retreatRepo.Object, tentRepo.Object, regRepo.Object, uow.Object);
    }

    [Fact]
    public async Task Should_throw_when_retreat_not_found()
    {
        var retId = Guid.NewGuid();
        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retId, It.IsAny<CancellationToken>())).ReturnsAsync((Retreat?)null);

        var tentRepo = new Mock<ITentRepository>();
        var regRepo = new Mock<IRegistrationRepository>();
        var uow = new Mock<IUnitOfWork>();

        var handler = new UpdateTentHandler(retreatRepo.Object, tentRepo.Object, regRepo.Object, uow.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateTentCommand(retId, Guid.NewGuid(), "10", TentCategory.Male, 2), CancellationToken.None));
    }

    [Fact]
    public async Task Should_throw_when_retreat_locked()
    {
        var ret = MakeRetreat(Guid.NewGuid(), 1, tentsLocked: true);
        var tent = MakeTent(ret.Id, TentCategory.Male, 1, 2);

        var handler = BuildHandler(ret, tent, assigned: 0, existsNumber: false);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.Handle(new UpdateTentCommand(ret.Id, Guid.NewGuid(), "2", TentCategory.Male, 2), CancellationToken.None));
    }

    [Fact]
    public async Task Should_throw_when_tent_not_found()
    {
        var ret = MakeRetreat(Guid.NewGuid(), 1, false);
        var handler = BuildHandler(ret, tent: null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateTentCommand(ret.Id, Guid.NewGuid(), "2", TentCategory.Male, 2), CancellationToken.None));
    }

    [Fact]
    public async Task Should_throw_when_tent_belongs_to_other_retreat()
    {
        var ret = MakeRetreat(Guid.NewGuid(), 1, false);
        var otherRet = Guid.NewGuid();
        var tent = MakeTent(otherRet, TentCategory.Male, 1, 2);

        var handler = BuildHandler(ret, tent);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateTentCommand(ret.Id, tent.Id, "3", TentCategory.Male, 2), CancellationToken.None));
    }

    [Fact]
    public async Task Should_throw_when_tent_is_locked()
    {
        var ret = MakeRetreat(Guid.NewGuid(), 1, false);
        var tent = MakeTent(ret.Id, TentCategory.Male, 1, 2, isLocked: true);

        var handler = BuildHandler(ret, tent);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.Handle(new UpdateTentCommand(ret.Id, tent.Id, "3", TentCategory.Male, 2), CancellationToken.None));
    }

    [Fact]
    public async Task Should_throw_when_new_capacity_less_than_assigned()
    {
        var ret = MakeRetreat(Guid.NewGuid(), 1, false);
        var tent = MakeTent(ret.Id, TentCategory.Male, 1, 3);

        var handler = BuildHandler(ret, tent, assigned: 2);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.Handle(new UpdateTentCommand(ret.Id, tent.Id, "1", TentCategory.Male, 1), CancellationToken.None));
    }

    [Fact]
    public async Task Should_throw_when_changing_category_with_assigned()
    {
        var ret = MakeRetreat(Guid.NewGuid(), 1, false);
        var tent = MakeTent(ret.Id, TentCategory.Male, 1, 3);

        var handler = BuildHandler(ret, tent, assigned: 1);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.Handle(new UpdateTentCommand(ret.Id, tent.Id, "1", TentCategory.Female, 3), CancellationToken.None));
    }

    [Fact]
    public async Task Should_throw_when_number_is_not_numeric()
    {
        var ret = MakeRetreat(Guid.NewGuid(), 1, false);
        var tent = MakeTent(ret.Id, TentCategory.Female, 5, 3);

        var handler = BuildHandler(ret, tent);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.Handle(new UpdateTentCommand(ret.Id, tent.Id, "abc", TentCategory.Female, 3), CancellationToken.None));
    }

    [Fact]
    public async Task Should_throw_when_number_already_exists()
    {
        var ret = MakeRetreat(Guid.NewGuid(), 1, false);
        var tent = MakeTent(ret.Id, TentCategory.Male, 2, 3);

        var handler = BuildHandler(ret, tent, assigned: 0, existsNumber: true);

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            handler.Handle(new UpdateTentCommand(ret.Id, tent.Id, "9", TentCategory.Male, 3), CancellationToken.None));
    }

    [Fact]
    public async Task Should_update_all_fields_and_bump_version()
    {
        var ret = MakeRetreat(Guid.NewGuid(), 10, false);
        var tent = MakeTent(ret.Id, TentCategory.Male, 1, 2, isLocked: false, isActive: true, notes: "old");

        var handler = BuildHandler(ret, tent, assigned: 0, existsNumber: false);

        var cmd = new UpdateTentCommand(
            RetreatId: ret.Id,
            TentId: tent.Id,
            Number: "99",
            Category: TentCategory.Female,
            Capacity: 5,
            IsActive: false,
            Notes: "  nova nota  "
        );

        var res = await handler.Handle(cmd, CancellationToken.None);

        res.RetreatId.Should().Be(ret.Id);
        res.TentId.Should().Be(tent.Id);
        res.Version.Should().Be(11);

        var num = (TentNumber)typeof(Tent).GetProperty("Number")!.GetValue(tent)!;
        num.Value.Should().Be(99);
        ((TentCategory)typeof(Tent).GetProperty("Category")!.GetValue(tent)!).Should().Be(TentCategory.Female);
        ((int)typeof(Tent).GetProperty("Capacity")!.GetValue(tent)!).Should().Be(5);
        ((bool)typeof(Tent).GetProperty("IsActive")!.GetValue(tent)!).Should().BeFalse();
        (((string?)typeof(Tent).GetProperty("Notes")!.GetValue(tent))!).Should().Be("nova nota");
    }

    [Fact]
    public async Task Should_allow_category_change_when_no_assigned()
    {
        var ret = MakeRetreat(Guid.NewGuid(), 2, false);
        var tent = MakeTent(ret.Id, TentCategory.Male, 4, 3);

        var handler = BuildHandler(ret, tent, assigned: 0, existsNumber: false);

        var res = await handler.Handle(new UpdateTentCommand(ret.Id, tent.Id, "4", TentCategory.Female, 3), CancellationToken.None);

        res.Version.Should().Be(3);
        ((TentCategory)typeof(Tent).GetProperty("Category")!.GetValue(tent)!).Should().Be(TentCategory.Female);
    }

    [Fact]
    public async Task Should_update_without_optional_fields_and_bump()
    {
        var ret = MakeRetreat(Guid.NewGuid(), 5, false);
        var tent = MakeTent(ret.Id, TentCategory.Male, 10, 2, isLocked: false, isActive: true, notes: "keep");

        var handler = BuildHandler(ret, tent, assigned: 0, existsNumber: false);

        var res = await handler.Handle(new UpdateTentCommand(ret.Id, tent.Id, "11", TentCategory.Male, 3, IsActive: null, Notes: null), CancellationToken.None);

        res.Version.Should().Be(6);
        var num = (TentNumber)typeof(Tent).GetProperty("Number")!.GetValue(tent)!;
        num.Value.Should().Be(11);
        ((bool)typeof(Tent).GetProperty("IsActive")!.GetValue(tent)!).Should().BeTrue();
        (((string?)typeof(Tent).GetProperty("Notes")!.GetValue(tent))!).Should().Be("keep");
    }
}
