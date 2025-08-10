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

public class ManualSelectionHandlersTests
{
    private static Retreat MakeRetreat(int maleSlots, int femaleSlots)
        => new Retreat(
            new FullName("Retiro Teste"),
            "ED1", "Tema",
            new DateOnly(2030,1,10), new DateOnly(2030,1,12),
            maleSlots, femaleSlots,
            new DateOnly(2020,1,1), new DateOnly(2040,1,1),
            new Money(0,"BRL"), new Money(0,"BRL"),
            new Percentage(50), new Percentage(50));

    [Fact]
    public async Task ManualSelect_respeita_limite_por_genero()
    {
        var retreatId = Guid.NewGuid();
        var regId1 = Guid.NewGuid();
        var regId2 = Guid.NewGuid();

        var retreat = MakeRetreat(1, 0);
        var reg1 = new Registration(new FullName("Teste 1"), new CPF("11111111111"), new EmailAddress("m1@x.com"),
            "11999999999", new DateOnly(2000,1,1), Gender.Male, "SP", RegistrationStatus.NotSelected,
            ParticipationCategory.Server, "Oeste", retreatId);
        typeof(Registration).GetProperty(nameof(Registration.Id))!.SetValue(reg1, regId1);

        var reg2 = new Registration(new FullName("Teste 2"), new CPF("22222222222"), new EmailAddress("m2@x.com"),
            "11999999999", new DateOnly(2000,1,1), Gender.Male, "SP", RegistrationStatus.NotSelected,
            ParticipationCategory.Guest, "Oeste", retreatId);
        typeof(Registration).GetProperty(nameof(Registration.Id))!.SetValue(reg2, regId2);

        var uow = new Mock<IUnitOfWork>();
        var retRepo = new Mock<IRetreatRepository>();
        var regRepo = new Mock<IRegistrationRepository>();

        retRepo.Setup(r => r.GetByIdAsync(retreatId, default)).ReturnsAsync(retreat);
        regRepo.Setup(r => r.GetByIdAsync(regId1, default)).ReturnsAsync(reg1);
        regRepo.Setup(r => r.GetByIdAsync(regId2, default)).ReturnsAsync(reg2);

        // zero ocupados no começo, depois 1 ocupado
        var occupied = 0;
        regRepo.Setup(r => r.CountByStatusesAndGenderAsync(retreatId, SlotPolicy.OccupyingStatuses, Gender.Male, default))
               .ReturnsAsync(() => occupied)
               .Callback(() => occupied = 1);

        var handler = new ManualSelectHandler(retRepo.Object, regRepo.Object, uow.Object);

        await handler.Handle(new ManualSelectCommand(retreatId, regId1), default);

        // segunda tentativa deve falhar pois ocupação já chegou no limite
        await FluentActions.Invoking(() => handler.Handle(new ManualSelectCommand(retreatId, regId2), default))
            .Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task ManualUnselect_retorna_para_NotSelected()
    {
        var retreatId = Guid.NewGuid();
        var regId = Guid.NewGuid();

        var reg = new Registration(new FullName("Teste 3"), new CPF("33333333333"), new EmailAddress("x@x.com"),
            "11999999999", new DateOnly(2000,1,1), Gender.Female, "SP", RegistrationStatus.Selected,
            ParticipationCategory.Guest, "Oeste", retreatId);
        typeof(Registration).GetProperty(nameof(Registration.Id))!.SetValue(reg, regId);

        var uow = new Mock<IUnitOfWork>();
        var regRepo = new Mock<IRegistrationRepository>();
        regRepo.Setup(r => r.GetByIdAsync(regId, default)).ReturnsAsync(reg);

        var handler = new ManualUnselectHandler(regRepo.Object, uow.Object);
        await handler.Handle(new ManualUnselectCommand(retreatId, regId), default);

        regRepo.Verify(r => r.UpdateStatusesAsync(
            It.Is<IEnumerable<Guid>>(ids => ids.Single() == regId),
            RegistrationStatus.NotSelected, default), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }
}
