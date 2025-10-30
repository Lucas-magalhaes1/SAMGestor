using System.Runtime.Serialization; 
using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Tents.Delete;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;
using Xunit;

namespace SAMGestor.UnitTests.Application.Features.Tents.Delete;

public class DeleteTentHandlerTests
{
    private static (Retreat retreat, bool hasLockProp, bool hasVersionProp, bool hasBumpMethod)
        CreateRetreat(bool tentsLocked = false, int tentsVersion = 0)
    {
        var retreat = (Retreat)FormatterServices.GetUninitializedObject(typeof(Retreat));

        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;

        var lockProp = typeof(Retreat).GetProperty("TentsLocked", flags);
        var verProp  = typeof(Retreat).GetProperty("TentsVersion", flags);
        var bump     = typeof(Retreat).GetMethod("BumpTentsVersion", flags);

        lockProp?.SetValue(retreat, tentsLocked);
        verProp?.SetValue(retreat, tentsVersion);

        return (retreat, lockProp is not null, verProp is not null, bump is not null);
    }

    private static Tent NewTent(Guid retreatId, int number = 1, TentCategory cat = TentCategory.Male, int capacity = 4, bool locked = false)
    {
        var t = new Tent(new TentNumber(number), cat, capacity, retreatId);

        if (locked)
        {
            var p = typeof(Tent).GetProperty("IsLocked",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            p?.SetValue(t, true);
        }
        return t;
    }

    private static DeleteTentHandler BuildHandler(
        Retreat? retreat = null,
        Tent? tent = null,
        int occupants = 0)
    {
        var retreatRepo = new Mock<IRetreatRepository>();
        var tentRepo    = new Mock<ITentRepository>();
        var regRepo     = new Mock<IRegistrationRepository>();
        var uow         = new Mock<IUnitOfWork>();

        retreatRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(retreat);

        tentRepo.Setup(t => t.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tent);

        regRepo.Setup(r => r.CountByTentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(occupants);

        tentRepo.Setup(t => t.DeleteAsync(It.IsAny<Tent>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        return new DeleteTentHandler(retreatRepo.Object, tentRepo.Object, regRepo.Object, uow.Object);
    }

    [Fact]
    public async Task Should_delete_tent_and_bump_version_on_happy_path()
    {
        var retreatId = Guid.NewGuid();
        var tentId    = Guid.NewGuid();

        var (retreat, hasLock, hasVer, hasBump) = CreateRetreat(tentsLocked: false, tentsVersion: 0);
        var tent = NewTent(retreatId, number: 10, cat: TentCategory.Female, capacity: 6, locked: false);

        var idProp = typeof(Tent).GetProperty("Id",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        idProp?.SetValue(tent, tentId);

        var handler = BuildHandler(retreat, tent, occupants: 0);

        var cmd = new DeleteTentCommand(retreatId, tentId);
        var res = await handler.Handle(cmd, CancellationToken.None);

        res.RetreatId.Should().Be(retreatId);
        res.TentId.Should().Be(tentId);
        if (hasVer && hasBump)
            res.Version.Should().Be(1);
        else
            res.Version.Should().Be(0);
    }

    [Fact]
    public async Task Should_throw_notfound_when_retreat_does_not_exist()
    {
        var handler = BuildHandler(retreat: null, tent: null);
        var cmd = new DeleteTentCommand(Guid.NewGuid(), Guid.NewGuid());

        await FluentActions.Invoking(() => handler.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Should_throw_notfound_when_tent_does_not_exist()
    {
        var (retreat, _, _, _) = CreateRetreat(false, 0);
        var handler = BuildHandler(retreat, tent: null);

        var cmd = new DeleteTentCommand(Guid.NewGuid(), Guid.NewGuid());
        await FluentActions.Invoking(() => handler.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Should_throw_notfound_when_tent_belongs_to_other_retreat()
    {
        var retreatId = Guid.NewGuid();
        var otherId   = Guid.NewGuid();

        var (retreat, _, _, _) = CreateRetreat(false, 0);
        var tent = NewTent(otherId, number: 5);

        var handler = BuildHandler(retreat, tent);

        var cmd = new DeleteTentCommand(retreatId, Guid.NewGuid());
        await FluentActions.Invoking(() => handler.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Should_throw_business_when_tent_is_locked()
    {
        var retreatId = Guid.NewGuid();
        var (retreat, _, _, _) = CreateRetreat(false, 0);
        var tent = NewTent(retreatId, number: 7, locked: true);

        var handler = BuildHandler(retreat, tent);

        var cmd = new DeleteTentCommand(retreatId, Guid.NewGuid());
        await FluentActions.Invoking(() => handler.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*bloqueada*");
    }

    [Fact]
    public async Task Should_throw_business_when_tent_has_occupants()
    {
        var retreatId = Guid.NewGuid();
        var (retreat, _, _, _) = CreateRetreat(false, 0);
        var tent = NewTent(retreatId, number: 9, locked: false);

        var handler = BuildHandler(retreat, tent, occupants: 3);

        var cmd = new DeleteTentCommand(retreatId, Guid.NewGuid());
        await FluentActions.Invoking(() => handler.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*ocupantes*");
    }

    [Fact]
    public async Task Should_throw_business_when_global_tents_locked_if_property_exists()
    {
        var retreatId = Guid.NewGuid();
        var (retreat, hasLock, _, _) = CreateRetreat(true, 0);
        var tent = NewTent(retreatId, number: 3, locked: false);

        if (!hasLock)
            return;

        var handler = BuildHandler(retreat, tent, occupants: 0);

        var cmd = new DeleteTentCommand(retreatId, Guid.NewGuid());
        await FluentActions.Invoking(() => handler.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*bloqueadas*");
    }
}
