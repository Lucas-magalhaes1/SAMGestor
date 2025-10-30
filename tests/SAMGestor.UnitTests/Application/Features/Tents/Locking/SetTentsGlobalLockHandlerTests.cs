using System.Reflection;
using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Tents.Locking;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using Xunit;

namespace SAMGestor.UnitTests.Application.Features.Tents.Locking;

public class SetTentsGlobalLockHandlerTests
{
    private static void SetBoolProp(object target, string name, bool value)
        => target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.SetValue(target, value);

    private static void SetId(object entity, Guid id)
        => entity.GetType().GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.SetValue(entity, id);

    private static Retreat NewRetreat(Guid id, bool locked = false)
    {
        var r = (Retreat)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Retreat));
        SetId(r, id);
        var prop = r.GetType().GetProperty("TentsLocked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        prop?.SetValue(r, locked);
        return r;
    }

    private static SetTentsGlobalLockHandler BuildHandler(
        out Mock<IRetreatRepository> retreatRepo,
        out Mock<IUnitOfWork> uow,
        Retreat? retreat = null)
    {
        retreatRepo = new Mock<IRetreatRepository>();
        uow         = new Mock<IUnitOfWork>();

        retreatRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(retreat!);

        retreatRepo.Setup(r => r.UpdateAsync(It.IsAny<Retreat>(), It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);

        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        return new SetTentsGlobalLockHandler(retreatRepo.Object, uow.Object);
    }

    [Fact]
    public async Task Should_lock_globally_when_unlocked()
    {
        var retreatId = Guid.NewGuid();
        var retreat = NewRetreat(retreatId, locked: false);

        var handler = BuildHandler(out var retreatRepo, out var uow, retreat);

        var cmd = new SetTentsGlobalLockCommand(retreatId, Lock: true);
        var res = await handler.Handle(cmd, CancellationToken.None);

        res.RetreatId.Should().Be(retreatId);
        res.Locked.Should().BeTrue();

        retreatRepo.Verify(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_unlock_globally_when_locked()
    {
        var retreatId = Guid.NewGuid();
        var retreat = NewRetreat(retreatId, locked: true);

        var handler = BuildHandler(out var retreatRepo, out var uow, retreat);

        var cmd = new SetTentsGlobalLockCommand(retreatId, Lock: false);
        var res = await handler.Handle(cmd, CancellationToken.None);

        res.Locked.Should().BeFalse();

        retreatRepo.Verify(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Should_throw_when_retreat_not_found()
    {
        var handler = BuildHandler(out var retreatRepo, out var uow, retreat: null);

        var cmd = new SetTentsGlobalLockCommand(Guid.NewGuid(), Lock: true);
        await FluentActions.Invoking(() => handler.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
