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

public class LockFamiliesHandlerTests
{
    private static Retreat NewRetreat(int initialVersion = 0, bool initiallyLocked = false)
    {
        var r = new Retreat(
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
        
        for (int i = 0; i < initialVersion; i++) r.BumpFamiliesVersion();
        if (initiallyLocked) r.LockFamilies(); 
        return r;
    }

    [Fact]
    public async Task Lock_true_trava_e_incrementa_versao()
    {
        
        var retreat = NewRetreat(initialVersion: 3, initiallyLocked: false); 
        var retRepo = new Mock<IRetreatRepository>();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);
        retRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        var handler = new SAMGestor.Application.Features.Families.Lock.LockFamiliesHandler(retRepo.Object, uow.Object);
        
        var res = await handler.Handle(new LockFamiliesCommand(retreat.Id, Lock: true), CancellationToken.None);
        
        res.Locked.Should().BeTrue();
        res.Version.Should().Be(retreat.FamiliesVersion);
        retRepo.Verify(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Lock_false_destrava_e_incrementa_versao()
    {
        
        var retreat = NewRetreat(initialVersion: 1, initiallyLocked: true);
        var previousVersion = retreat.FamiliesVersion;

        var retRepo = new Mock<IRetreatRepository>();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);
        retRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        var handler = new SAMGestor.Application.Features.Families.Lock.LockFamiliesHandler(retRepo.Object, uow.Object);
        
        var res = await handler.Handle(new LockFamiliesCommand(retreat.Id, Lock: false), CancellationToken.None);
        
        res.Locked.Should().BeFalse();
        res.Version.Should().Be(retreat.FamiliesVersion);
        retreat.FamiliesVersion.Should().BeGreaterThan(previousVersion);
        retRepo.Verify(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotFound_quando_retiro_inexistente()
    {
        
        var retRepo = new Mock<IRetreatRepository>();
        retRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((Retreat?)null);

        var handler = new SAMGestor.Application.Features.Families.Lock.LockFamiliesHandler(retRepo.Object, new Mock<IUnitOfWork>().Object);
        
        await FluentActions.Invoking(() =>
                handler.Handle(new LockFamiliesCommand(Guid.NewGuid(), Lock: true), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Retreat*");
    }
}
