using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Families.Groups.Status;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Application.Features.Families.Groups.Status;

public class GetGroupsStatusSummaryHandlerTests
{
    private static Retreat OpenRetreat()
        => new Retreat(
            new FullName("Retiro Status"), "ED1", "Tema",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12)),
            50, 50,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            new Money(0,"BRL"), new Money(0,"BRL"),
            new Percentage(50), new Percentage(50));

    private static Family Fam(Guid retreatId, string name, GroupStatus status)
    {
        var f = new Family(new FamilyName(name), retreatId, 4, FamilyColor.FromName("Azul"));
        switch (status)
        {
            case GroupStatus.Creating: f.MarkGroupCreating(); break;
            case GroupStatus.Active:   f.MarkGroupActive("https://chat/x","ext","whatsapp",DateTimeOffset.UtcNow,null); break;
            case GroupStatus.Failed:   f.MarkGroupFailed(); break;
            case GroupStatus.None: default: break;
        }
        return f;
    }

    private static GetGroupsStatusSummaryHandler BuildHandler(
        Retreat retreat,
        List<Family> families,
        out Mock<IRetreatRepository> retRepo,
        out Mock<IFamilyRepository> famRepo)
    {
        retRepo = new Mock<IRetreatRepository>();
        famRepo = new Mock<IFamilyRepository>();

        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>())).ReturnsAsync(retreat);
        famRepo.Setup(r => r.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>())).ReturnsAsync(families);

        return new GetGroupsStatusSummaryHandler(retRepo.Object, famRepo.Object);
    }

    [Fact]
    public async Task Falha_quando_retiro_nao_existe()
    {
        var retreatId = Guid.NewGuid();
        var retRepo = new Mock<IRetreatRepository>();
        var famRepo = new Mock<IFamilyRepository>();
        retRepo.Setup(r => r.GetByIdAsync(retreatId, It.IsAny<CancellationToken>())).ReturnsAsync((Retreat?)null);

        var sut = new GetGroupsStatusSummaryHandler(retRepo.Object, famRepo.Object);

        await FluentActions.Invoking(() => sut.Handle(new GetGroupsStatusSummaryQuery(retreatId), default))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Retreat*");
    }

    [Fact]
    public async Task Retorna_zeros_quando_sem_familias()
    {
        var retreat = OpenRetreat();
        var sut = BuildHandler(retreat, new List<Family>(), out var retRepo, out var famRepo);

        var res = await sut.Handle(new GetGroupsStatusSummaryQuery(retreat.Id), default);

        res.TotalFamilies.Should().Be(0);
        res.None.Should().Be(0);
        res.Creating.Should().Be(0);
        res.Active.Should().Be(0);
        res.Failed.Should().Be(0);

        retRepo.Verify(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()), Times.Once);
        famRepo.Verify(r => r.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Conta_por_status_corretamente()
    {
        var retreat = OpenRetreat();
        var f1 = Fam(retreat.Id, "Fam A", GroupStatus.None);
        var f2 = Fam(retreat.Id, "Fam B", GroupStatus.Creating);
        var f3 = Fam(retreat.Id, "Fam C", GroupStatus.Creating);
        var f4 = Fam(retreat.Id, "Fam D", GroupStatus.Active);
        var f5 = Fam(retreat.Id, "Fam E", GroupStatus.Failed);
        var f6 = Fam(retreat.Id, "Fam F", GroupStatus.Failed);

        var sut = BuildHandler(retreat, new List<Family> { f1, f2, f3, f4, f5, f6 }, out _, out _);

        var res = await sut.Handle(new GetGroupsStatusSummaryQuery(retreat.Id), default);

        res.TotalFamilies.Should().Be(6);
        res.None.Should().Be(1);
        res.Creating.Should().Be(2);
        res.Active.Should().Be(1);
        res.Failed.Should().Be(2);
    }
}
