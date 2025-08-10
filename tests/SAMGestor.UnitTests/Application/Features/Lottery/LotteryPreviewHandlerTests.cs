using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Lottery;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Application.Features.Lottery;

public class LotteryPreviewHandlerTests
{
    private static Retreat MakeRetreat(int maleSlots, int femaleSlots, bool closed = false)
        => new Retreat(
            new FullName("Retiro Teste"),
            "ED1",
            "Tema",
            new DateOnly(2030, 1, 10),
            new DateOnly(2030, 1, 12),
            maleSlots, femaleSlots,
            new DateOnly(2020, 1, 1),
            new DateOnly(2040, 1, 1),
            new Money(0, "BRL"),
            new Money(0, "BRL"),
            new Percentage(50),
            new Percentage(50)
        );

    [Fact]
    public async Task Preview_respeita_capacidade_por_genero()
    {
        // Arrange
        var retreatId = Guid.NewGuid();
        var retreat = MakeRetreat(2, 1);

        var retRepo = new Mock<IRetreatRepository>();
        retRepo.Setup(r => r.GetByIdAsync(retreatId, default)).ReturnsAsync(retreat);

        var regRepo = new Mock<IRegistrationRepository>();
        // nenhum selecionado ainda
        regRepo.Setup(r => r.CountByStatusesAndGenderAsync(retreatId, SlotPolicy.OccupyingStatuses, Gender.Male, default)).ReturnsAsync(0);
        regRepo.Setup(r => r.CountByStatusesAndGenderAsync(retreatId, SlotPolicy.OccupyingStatuses, Gender.Female, default)).ReturnsAsync(0);

        // pools
        regRepo.Setup(r => r.ListAppliedIdsByGenderAsync(retreatId, Gender.Male, default))
               .ReturnsAsync(new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() });
        regRepo.Setup(r => r.ListAppliedIdsByGenderAsync(retreatId, Gender.Female, default))
               .ReturnsAsync(new List<Guid> { Guid.NewGuid(), Guid.NewGuid() });

        var handler = new LotteryPreviewHandler(retRepo.Object, regRepo.Object);

        // Act
        var result = await handler.Handle(new LotteryPreviewQuery(retreatId), default);

        // Assert
        result.Male.Should().HaveCount(2);
        result.Female.Should().HaveCount(1);
        result.MaleCap.Should().Be(2);
        result.FemaleCap.Should().Be(1);
    }

    [Fact]
    public async Task Preview_falha_se_contemplacao_fechada()
    {
        var retreatId = Guid.NewGuid();
        var retreat = MakeRetreat(1, 1);
        retreat.CloseContemplation();

        var retRepo = new Mock<IRetreatRepository>();
        retRepo.Setup(r => r.GetByIdAsync(retreatId, default)).ReturnsAsync(retreat);

        var regRepo = new Mock<IRegistrationRepository>();
        var handler = new LotteryPreviewHandler(retRepo.Object, regRepo.Object);

        await FluentActions
            .Invoking(() => handler.Handle(new LotteryPreviewQuery(retreatId), default))
            .Should().ThrowAsync<BusinessRuleException>();
    }
}
