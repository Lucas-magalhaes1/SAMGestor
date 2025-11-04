using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Registrations.Create;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Application.Features.Registrations.Create;

public class CreateRegistrationHandlerTests
{
    private static Retreat OpenRetreat()
        => new Retreat(
            new FullName("Retiro Teste"),
            "ED1",
            "Tema",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(32)),
            10,
            10,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            new Money(0, "BRL"),
            new Money(0, "BRL"),
            new Percentage(50),
            new Percentage(50));

    private static Retreat ClosedRetreat()
        => new Retreat(
            new FullName("Retiro Fechado"),
            "ED1",
            "Tema",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(32)),
            10,
            10,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
            new Money(0, "BRL"),
            new Money(0, "BRL"),
            new Percentage(50),
            new Percentage(50));

    private static CreateRegistrationCommand NewCmd(Guid retreatId) =>
        new CreateRegistrationCommand(
            // básicos
            RetreatId: retreatId,
            Name: new FullName("Fulano Ciclano"),
            Cpf: new CPF("52998224725"),
            Email: new EmailAddress("f@x.com"),
            Phone: "11999999999",
            BirthDate: new DateOnly(2000, 1, 1),
            Gender: Gender.Male,
            City: "São Paulo",

            // pessoais
            MaritalStatus: MaritalStatus.Married,
            Pregnancy: PregnancyStatus.None,      // Gender = Male, então None
            ShirtSize: ShirtSize.M,
            WeightKg: 80.0m,
            HeightCm: 180.0m,
            Profession: "Dev",
            StreetAndNumber: "Rua A, 123",
            Neighborhood: "Centro",
            State: UF.SP,

            // contatos
            Whatsapp: "11988887777",
            FacebookUsername: "fulano.fb",
            InstagramHandle: "fulano.ig",
            NeighborPhone: "1133334444",
            RelativePhone: "11911112222",

            // família
            FatherStatus: ParentStatus.Alive,
            FatherName: "Pai Fulano",
            FatherPhone: "1133332222",
            MotherStatus: ParentStatus.Alive,
            MotherName: "Mãe Fulana",
            MotherPhone: "11911113333",
            HadFamilyLossLast6Months: false,
            FamilyLossDetails: null,
            HasRelativeOrFriendSubmitted: false,
            SubmitterRelationship: RelationshipDegree.None,
            SubmitterNames: null,

            // religião/histórico
            Religion: "Católica",
            PreviousUncalledApplications: RahaminAttempt.None,
            RahaminVidaCompleted: RahaminVidaEdition.None,

            // saúde
            AlcoholUse: AlcoholUsePattern.None,
            Smoker: false,
            UsesDrugs: false,
            DrugUseFrequency: null,
            HasAllergies: false,
            AllergiesDetails: null,
            HasMedicalRestriction: false,
            MedicalRestrictionDetails: null,
            TakesMedication: false,
            MedicationsDetails: null,
            PhysicalLimitationDetails: null,
            RecentSurgeryOrProcedureDetails: null,

            // consentimentos/contexto
            TermsAccepted: true,
            TermsVersion: "2025-10-01",
            MarketingOptIn: true,
            ClientIp: "127.0.0.1",
            UserAgent: "UnitTest"
        );

    [Fact]
    public async Task Success_cria_e_salva()
    {
        var retreat = OpenRetreat();

        var retRepo = new Mock<IRetreatRepository>();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        var regRepo = new Mock<IRegistrationRepository>();
        regRepo.Setup(r => r.IsCpfBlockedAsync(It.IsAny<CPF>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(false);
        regRepo.Setup(r => r.ExistsByCpfInRetreatAsync(It.IsAny<CPF>(), retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(false);
        regRepo.Setup(r => r.AddAsync(It.IsAny<Registration>(), It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        var handler = new CreateRegistrationHandler(regRepo.Object, retRepo.Object, uow.Object);

        var res = await handler.Handle(NewCmd(retreat.Id), default);

        res.RegistrationId.Should().NotBeEmpty();
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        regRepo.Verify(r => r.AddAsync(It.IsAny<Registration>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Falha_quando_periodo_fechado()
    {
        var retreat = ClosedRetreat();

        var retRepo = new Mock<IRetreatRepository>();
        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);

        var handler = new CreateRegistrationHandler(
            new Mock<IRegistrationRepository>().Object,
            retRepo.Object,
            new Mock<IUnitOfWork>().Object);

        await FluentActions.Invoking(() => handler.Handle(NewCmd(retreat.Id), default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*Registration period closed*");
    }
}
