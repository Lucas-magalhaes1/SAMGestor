using FluentAssertions;
using MediatR;
using Moq;
using SAMGestor.Application.Features.Lottery;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;
using System.Data;

namespace SAMGestor.UnitTests.Application.Features.Lottery;

public class LotteryCommitHandlerTests
{
    private static Retreat MakeRetreat()
        => new Retreat(
            new FullName("Retiro Teste"),
            "ED1", "Tema",
            new DateOnly(2030, 1, 10), new DateOnly(2030, 1, 12),
            1, 1,
            new DateOnly(2020, 1, 1), new DateOnly(2040, 1, 1),
            new Money(0, "BRL"), new Money(0, "BRL"),
            new Percentage(50), new Percentage(50)
        );

    [Fact]
    public async Task Commit_aplica_preview_nos_status_e_comita_transacao()
    {
        // Arrange
        var retreatId = Guid.NewGuid();
        var retreat = MakeRetreat();

        var uow = new Mock<IUnitOfWork>();
        var retRepo = new Mock<IRetreatRepository>();
        var regRepo = new Mock<IRegistrationRepository>();
        var mediator = new Mock<IMediator>();

        retRepo.Setup(r => r.GetByIdAsync(retreatId, default)).ReturnsAsync(retreat);

        // Corrigido: Criar como List<Guid> ao invés de array
        var malePicked = new List<Guid> { Guid.NewGuid() };
        var femalePicked = new List<Guid> { Guid.NewGuid() };
    
        mediator.Setup(m => m.Send(It.Is<LotteryPreviewQuery>(q => q.RetreatId == retreatId), default))
            .ReturnsAsync(new LotteryResultDto(
                malePicked, 
                femalePicked, 
                1, 
                1,
                null,  // PriorityCities
                null   // AgeRange
            ));

        var handler = new LotteryCommitHandler(mediator.Object, uow.Object, retRepo.Object, regRepo.Object);

        // Act
        var result = await handler.Handle(new LotteryCommitCommand(retreatId), default);

        // Assert
        result.Male.Should().ContainSingle();
        result.Female.Should().ContainSingle();

        uow.Verify(u => u.BeginTransactionAsync(IsolationLevel.Serializable, default), Times.Once);
        regRepo.Verify(r => r.UpdateStatusesAsync(
            It.Is<IEnumerable<Guid>>(ids => ids.Contains(malePicked[0]) && ids.Contains(femalePicked[0])),
            RegistrationStatus.Selected, default), Times.Once);
        uow.Verify(u => u.CommitTransactionAsync(default), Times.Once);
    }

    [Fact]
    public async Task Commit_falha_se_contemplacao_fechada()
    {
        var retreatId = Guid.NewGuid();
        var retreat = MakeRetreat();
        retreat.CloseContemplation();

        var uow = new Mock<IUnitOfWork>();
        var retRepo = new Mock<IRetreatRepository>();
        var regRepo = new Mock<IRegistrationRepository>();
        var mediator = new Mock<IMediator>();

        retRepo.Setup(r => r.GetByIdAsync(retreatId, default)).ReturnsAsync(retreat);

        var handler = new LotteryCommitHandler(mediator.Object, uow.Object, retRepo.Object, regRepo.Object);

        await FluentActions.Invoking(() => handler.Handle(new LotteryCommitCommand(retreatId), default))
            .Should().ThrowAsync<BusinessRuleException>();
    }
}
