using System.Reflection;
using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Tents.Locking;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;
using SAMGestor.Domain.Enums;
using SAMGestor.UnitTests.Dependencies;
using Xunit;

namespace SAMGestor.UnitTests.Application.Features.Tents.Locking;

public class SetTentLockHandlerTests
{
    private static Tent NewTent(Guid retreatId, bool isLocked)
    {
        var t = new Tent(new TentNumber(1), TentCategory.Male, 4, retreatId);
        typeof(Tent).GetProperty("IsLocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.SetValue(t, isLocked);
        return t;
    }

    private static void SetId(object entity, Guid id)
        => entity.GetType().GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.SetValue(entity, id);

    private static Retreat NewRetreat(Guid id)
    {
        var r = TestObjectFactory.Uninitialized<Retreat>();
        SetId(r, id);
        return r;
    }

    private static SetTentLockHandler BuildHandler(
        out Mock<IRetreatRepository> retreatRepo,
        out Mock<ITentRepository> tentRepo,
        out Mock<IUnitOfWork> uow,
        Retreat? retreat = null,
        Tent? tent = null)
    {
        retreatRepo = new Mock<IRetreatRepository>();
        tentRepo    = new Mock<ITentRepository>();
        uow         = new Mock<IUnitOfWork>();

        retreatRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(retreat!);

        tentRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tent!);

        tentRepo.Setup(r => r.UpdateAsync(It.IsAny<Tent>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        return new SetTentLockHandler(retreatRepo.Object, tentRepo.Object, uow.Object);
    }

    [Fact]
    public async Task Should_lock_when_unlocked()
    {
        var retreatId = Guid.NewGuid();
        var tentId = Guid.NewGuid();
        var retreat = NewRetreat(retreatId);
        var tent = NewTent(retreatId, isLocked: false);
        SetId(tent, tentId);

        var handler = BuildHandler(out var retreatRepo, out var tentRepo, out var uow, retreat, tent);

        var cmd = new SetTentLockCommand(retreatId, tentId, Lock: true);
        var res = await handler.Handle(cmd, CancellationToken.None);

        res.RetreatId.Should().Be(retreatId);
        res.TentId.Should().Be(tentId);
        res.Locked.Should().BeTrue();

        tentRepo.Verify(r => r.UpdateAsync(tent, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_unlock_when_locked()
    {
        var retreatId = Guid.NewGuid();
        var tentId = Guid.NewGuid();
        var retreat = NewRetreat(retreatId);
        var tent = NewTent(retreatId, isLocked: true);
        SetId(tent, tentId);

        var handler = BuildHandler(out var retreatRepo, out var tentRepo, out var uow, retreat, tent);

        var cmd = new SetTentLockCommand(retreatId, tentId, Lock: false);
        var res = await handler.Handle(cmd, CancellationToken.None);

        res.Locked.Should().BeFalse();
        tentRepo.Verify(r => r.UpdateAsync(tent, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Noop_when_state_is_already_desired()
    {
        var retreatId = Guid.NewGuid();
        var tentId = Guid.NewGuid();
        var retreat = NewRetreat(retreatId);
        var tent = NewTent(retreatId, isLocked: false);
        SetId(tent, tentId);

        var handler = BuildHandler(out var retreatRepo, out var tentRepo, out var uow, retreat, tent);

        var cmd = new SetTentLockCommand(retreatId, tentId, Lock: false);
        var res = await handler.Handle(cmd, CancellationToken.None);

        res.Locked.Should().BeFalse();
        tentRepo.Verify(r => r.UpdateAsync(tent, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_throw_when_tent_not_found()
    {
        var retreatId = Guid.NewGuid();
        var retreat = NewRetreat(retreatId);

        var handler = BuildHandler(out var retreatRepo, out var tentRepo, out var uow, retreat, tent: null);

        var cmd = new SetTentLockCommand(retreatId, Guid.NewGuid(), Lock: true);
        await FluentActions.Invoking(() => handler.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Should_throw_when_tent_belongs_to_other_retreat()
    {
        var retreatId = Guid.NewGuid();
        var retreat = NewRetreat(retreatId);
        var tent = NewTent(Guid.NewGuid(), isLocked: false);
        SetId(tent, Guid.NewGuid());

        var handler = BuildHandler(out var retreatRepo, out var tentRepo, out var uow, retreat, tent);

        var cmd = new SetTentLockCommand(retreatId, tent.Id, Lock: true);
        await FluentActions.Invoking(() => handler.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
