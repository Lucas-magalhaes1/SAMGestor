using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Families.Lock;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;
using Xunit;

namespace SAMGestor.UnitTests.Application.Features.Families.Lock;

public sealed class LockSingleFamilyHandlerUnitTests
{
    private static Retreat NewRetreat()
        => new Retreat(
            new FullName("Retiro Teste"),
            "ED1",
            "Tema",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12)),
            10, 10,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            new Money(0, "BRL"),
            new Money(0, "BRL"),
            new Percentage(50),
            new Percentage(50));

    private static Family NewFamily(Guid retreatId, string name = "Família 1", int capacity = 4)
        => new Family(new FamilyName(name), retreatId, capacity);

    [Fact]
    public async Task Lock_true_trava_familia_e_bumpa_versao()
    {
        
        var retreat = NewRetreat();
        var family  = NewFamily(retreat.Id);

        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(retreat);
        retreatRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);

        var familyRepo = new Mock<IFamilyRepository>();
        familyRepo.Setup(f => f.GetByIdAsync(family.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(family);
        familyRepo.Setup(f => f.UpdateAsync(family, It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        var handler = new LockSingleFamilyHandler(retreatRepo.Object, familyRepo.Object, uow.Object);
        
        var res = await handler.Handle(new LockSingleFamilyCommand(retreat.Id, family.Id, Lock: true), CancellationToken.None);
        
        res.FamilyId.Should().Be(family.Id);
        res.Locked.Should().BeTrue();
        res.Version.Should().Be(retreat.FamiliesVersion);
        family.IsLocked.Should().BeTrue();

        familyRepo.Verify(f => f.UpdateAsync(family, It.IsAny<CancellationToken>()), Times.Once);
        retreatRepo.Verify(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Lock_false_destrava_familia_e_bumpa_versao()
    {
        
        var retreat = NewRetreat();
        var family  = NewFamily(retreat.Id);
        family.Lock(); 

        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(retreat);
        retreatRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()))
                   .Returns(Task.CompletedTask);

        var familyRepo = new Mock<IFamilyRepository>();
        familyRepo.Setup(f => f.GetByIdAsync(family.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(family);
        familyRepo.Setup(f => f.UpdateAsync(family, It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        var handler = new LockSingleFamilyHandler(retreatRepo.Object, familyRepo.Object, uow.Object);
        
        var res = await handler.Handle(new LockSingleFamilyCommand(retreat.Id, family.Id, Lock: false), CancellationToken.None);
        
        res.FamilyId.Should().Be(family.Id);
        res.Locked.Should().BeFalse();
        res.Version.Should().Be(retreat.FamiliesVersion);
        family.IsLocked.Should().BeFalse();

        familyRepo.Verify(f => f.UpdateAsync(family, It.IsAny<CancellationToken>()), Times.Once);
        retreatRepo.Verify(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotFound_quando_retiro_nao_existe()
    {
        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((Retreat?)null);

        var handler = new LockSingleFamilyHandler(retreatRepo.Object, new Mock<IFamilyRepository>().Object, new Mock<IUnitOfWork>().Object);

        await FluentActions.Invoking(() =>
                handler.Handle(new LockSingleFamilyCommand(Guid.NewGuid(), Guid.NewGuid(), Lock: true), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Retreat*");
    }

    [Fact]
    public async Task NotFound_quando_familia_nao_existe()
    {
        var retreat = NewRetreat();

        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(retreat);

        var familyRepo = new Mock<IFamilyRepository>();
        familyRepo.Setup(f => f.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((Family?)null);

        var handler = new LockSingleFamilyHandler(retreatRepo.Object, familyRepo.Object, new Mock<IUnitOfWork>().Object);

        await FluentActions.Invoking(() =>
                handler.Handle(new LockSingleFamilyCommand(retreat.Id, Guid.NewGuid(), Lock: true), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Family*");
    }

    [Fact]
    public async Task BusinessRule_quando_familia_de_outro_retiro()
    {
        var retreatA = NewRetreat();
        var retreatB = NewRetreat();
        var familyB  = NewFamily(retreatB.Id); 

        var retreatRepo = new Mock<IRetreatRepository>();
        retreatRepo.Setup(r => r.GetByIdAsync(retreatA.Id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(retreatA);

        var familyRepo = new Mock<IFamilyRepository>();
        familyRepo.Setup(f => f.GetByIdAsync(familyB.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(familyB);

        var handler = new LockSingleFamilyHandler(retreatRepo.Object, familyRepo.Object, new Mock<IUnitOfWork>().Object);

        await FluentActions.Invoking(() =>
                handler.Handle(new LockSingleFamilyCommand(retreatA.Id, familyB.Id, Lock: true), CancellationToken.None))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*não pertence ao retiro*");
    }
}
