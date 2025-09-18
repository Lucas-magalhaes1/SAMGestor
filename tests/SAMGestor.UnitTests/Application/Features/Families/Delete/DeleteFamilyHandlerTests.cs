using FluentAssertions;
using MediatR;
using Moq;
using SAMGestor.Application.Features.Families.Delete;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;


namespace SAMGestor.UnitTests.Application.Features.Families.Delete;

public class DeleteFamilyHandlerTests
{
    private static Retreat NewRetreat() => new Retreat(
        new FullName("Retiro X"), "ED1", "Tema",
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12)),
        10, 10,
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
        new Money(0,"BRL"), new Money(0,"BRL"),
        new Percentage(50), new Percentage(50));

    private static Family NewFamily(Guid retreatId, string name = "Família 1", int capacity = 4)
        => new Family(new FamilyName(name), retreatId, capacity);

    private static DeleteFamilyHandler BuildHandler(
        Retreat retreat,
        out Mock<IRetreatRepository> retRepo,
        out Mock<IFamilyRepository> familyRepo,
        out Mock<IUnitOfWork> uow)
    {
        retRepo = new Mock<IRetreatRepository>();
        familyRepo = new Mock<IFamilyRepository>();
        uow = new Mock<IUnitOfWork>();

        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        retRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        return new DeleteFamilyHandler(retRepo.Object, familyRepo.Object, uow.Object);
    }

    [Fact]
    public async Task Falha_quando_retiro_nao_existe()
    {
        var retRepo = new Mock<IRetreatRepository>();
        retRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((Retreat?)null);

        var handler = new DeleteFamilyHandler(retRepo.Object, new Mock<IFamilyRepository>().Object, new Mock<IUnitOfWork>().Object);

        await FluentActions.Invoking(() =>
            handler.Handle(new DeleteFamilyCommand(Guid.NewGuid(), Guid.NewGuid()), default))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Retreat*");
    }

    [Fact]
    public async Task Falha_quando_lock_global()
    {
        var retreat = NewRetreat();
        retreat.LockFamilies();

        var handler = BuildHandler(retreat, out _, out _, out _);

        await FluentActions.Invoking(() =>
            handler.Handle(new DeleteFamilyCommand(retreat.Id, Guid.NewGuid()), default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*bloqueadas*");
    }

    [Fact]
    public async Task Falha_quando_familia_nao_existe()
    {
        var retreat = NewRetreat();
        var handler = BuildHandler(retreat, out _, out var familyRepo, out _);

        familyRepo.Setup(f => f.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((Family?)null);

        await FluentActions.Invoking(() =>
            handler.Handle(new DeleteFamilyCommand(retreat.Id, Guid.NewGuid()), default))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Family*");
    }

    [Fact]
    public async Task Falha_quando_familia_de_outro_retiro()
    {
        var retreat = NewRetreat();
        var other = NewRetreat();

        var fam = NewFamily(other.Id);

        var handler = BuildHandler(retreat, out _, out var familyRepo, out _);
        familyRepo.Setup(f => f.GetByIdAsync(fam.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(fam);

        await FluentActions.Invoking(() =>
            handler.Handle(new DeleteFamilyCommand(retreat.Id, fam.Id), default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*não pertence ao retiro*");
    }

    [Fact]
    public async Task Falha_quando_familia_lockada()
    {
        var retreat = NewRetreat();
        var fam = NewFamily(retreat.Id);
        fam.Lock(); 

        var handler = BuildHandler(retreat, out _, out var familyRepo, out _);
        familyRepo.Setup(f => f.GetByIdAsync(fam.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(fam);

        await FluentActions.Invoking(() =>
            handler.Handle(new DeleteFamilyCommand(retreat.Id, fam.Id), default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*bloqueada*");
    }

    [Fact]
    public async Task Sucesso_deleta_bumpa_versao_e_persiste()
    {
        var retreat = NewRetreat();
        var fam = NewFamily(retreat.Id);

        var handler = BuildHandler(retreat, out var retRepo, out var familyRepo, out var uow);

        familyRepo.Setup(f => f.GetByIdAsync(fam.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(fam);

        familyRepo.Setup(f => f.DeleteAsync(fam, It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);

        var before = retreat.FamiliesVersion;

        var res = await handler.Handle(new DeleteFamilyCommand(retreat.Id, fam.Id), default);
        res.Should().Be(Unit.Value);

        familyRepo.Verify(f => f.DeleteAsync(fam, It.IsAny<CancellationToken>()), Times.Once);
        retRepo.Verify(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        retreat.FamiliesVersion.Should().Be(before + 1);
    }
}
