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
    private static Retreat MakeRetreat(int maleSlots = 10, int femaleSlots = 10)
        => new Retreat(
            new FullName("Retiro Teste"),
            "ED1", "Tema",
            new DateOnly(2030, 1, 10), // StartDate (usado pra calcular idade)
            new DateOnly(2030, 1, 12),
            maleSlots, femaleSlots,
            new DateOnly(2020, 1, 1), new DateOnly(2040, 1, 1),
            new Money(0, "BRL"), new Money(0, "BRL"),
            new Percentage(50), new Percentage(50)
        );

    private static Registration MakeReg(
        Guid retreatId,
        string name,
        Gender gender,
        string city = "São Paulo",
        int birthYear = 2000)
        => new Registration(
            new FullName(name),
            new CPF("52998224725"),
            new EmailAddress($"{Guid.NewGuid():N}@test.com"),
            "11999999999",
            new DateOnly(birthYear, 1, 1),
            gender,
            city,
            RegistrationStatus.NotSelected,
            retreatId
        );

    [Fact]
    public async Task Preview_sem_prioridades_embaralha_todos()
    {
        // Arrange
        var retreat = MakeRetreat(maleSlots: 2, femaleSlots: 2);
        var retreatId = retreat.Id;

        var m1 = MakeReg(retreatId, "Joao Silva", Gender.Male, "Recife");
        var m2 = MakeReg(retreatId, "Pedro Costa", Gender.Male, "Fortaleza");
        var f1 = MakeReg(retreatId, "Maria Souza", Gender.Female, "Recife");
        var f2 = MakeReg(retreatId, "Ana Lima", Gender.Female, "São Paulo");

        var allApplied = new List<Registration> { m1, m2, f1, f2 };

        var retRepo = new Mock<IRetreatRepository>();
        var regRepo = new Mock<IRegistrationRepository>();

        retRepo.Setup(r => r.GetByIdAsync(retreatId, default)).ReturnsAsync(retreat);
        regRepo.Setup(r => r.CountByStatusesAndGenderAsync(retreatId, It.IsAny<RegistrationStatus[]>(), Gender.Male, default))
               .ReturnsAsync(0);
        regRepo.Setup(r => r.CountByStatusesAndGenderAsync(retreatId, It.IsAny<RegistrationStatus[]>(), Gender.Female, default))
               .ReturnsAsync(0);
        regRepo.Setup(r => r.ListAppliedByGenderAsync(retreatId, default))
               .ReturnsAsync(allApplied);

        var handler = new LotteryPreviewHandler(retRepo.Object, regRepo.Object);

        // Act
        var query = new LotteryPreviewQuery(retreatId);
        var result = await handler.Handle(query, default);

        // Assert
        result.Male.Should().HaveCount(2);
        result.Female.Should().HaveCount(2);
        result.MalePriority.Should().BeEmpty();   // Sem prioridades
        result.FemalePriority.Should().BeEmpty();
    }

    [Fact]
    public async Task Preview_com_prioridade_cidade_seleciona_prioritarios_primeiro()
    {
        // Arrange
        var retreat = MakeRetreat(maleSlots: 2, femaleSlots: 1);
        var retreatId = retreat.Id;

        var m1 = MakeReg(retreatId, "Joao Silva", Gender.Male, "Recife");      // Prioritário
        var m2 = MakeReg(retreatId, "Pedro Costa", Gender.Male, "São Paulo");  // Regular
        var m3 = MakeReg(retreatId, "Carlos Dias", Gender.Male, "Fortaleza");  // Prioritário
        var f1 = MakeReg(retreatId, "Maria Souza", Gender.Female, "Recife");   // Prioritária

        var allApplied = new List<Registration> { m1, m2, m3, f1 };

        var retRepo = new Mock<IRetreatRepository>();
        var regRepo = new Mock<IRegistrationRepository>();

        retRepo.Setup(r => r.GetByIdAsync(retreatId, default)).ReturnsAsync(retreat);
        regRepo.Setup(r => r.CountByStatusesAndGenderAsync(retreatId, It.IsAny<RegistrationStatus[]>(), Gender.Male, default))
               .ReturnsAsync(0);
        regRepo.Setup(r => r.CountByStatusesAndGenderAsync(retreatId, It.IsAny<RegistrationStatus[]>(), Gender.Female, default))
               .ReturnsAsync(0);
        regRepo.Setup(r => r.ListAppliedByGenderAsync(retreatId, default))
               .ReturnsAsync(allApplied);

        var handler = new LotteryPreviewHandler(retRepo.Object, regRepo.Object);

        // Act
        var query = new LotteryPreviewQuery(retreatId, PriorityCities: new List<string> { "Recife", "Fortaleza" });
        var result = await handler.Handle(query, default);

        // Assert
        result.Male.Should().HaveCount(2);
        result.MalePriority.Should().HaveCount(2); // m1 e m3 (Recife e Fortaleza)
        result.MalePriority.Should().Contain(m1.Id);
        result.MalePriority.Should().Contain(m3.Id);
        
        result.Female.Should().ContainSingle();
        result.FemalePriority.Should().ContainSingle();
        result.FemalePriority.Should().Contain(f1.Id);
    }

    [Fact]
    public async Task Preview_com_prioridade_idade_seleciona_faixa_etaria()
    {
        // Arrange
        var retreat = MakeRetreat(maleSlots: 3, femaleSlots: 1);
        var retreatId = retreat.Id;

        // Idades em 2030-01-10: 30, 22, 40, 20 anos
        var m1 = MakeReg(retreatId, "Joao Silva", Gender.Male, birthYear: 2000);   // 30 anos - fora
        var m2 = MakeReg(retreatId, "Pedro Costa", Gender.Male, birthYear: 2008);  // 22 anos - dentro (18-25)
        var m3 = MakeReg(retreatId, "Carlos Dias", Gender.Male, birthYear: 1990);  // 40 anos - fora
        var f1 = MakeReg(retreatId, "Maria Souza", Gender.Female, birthYear: 2010); // 20 anos - dentro

        var allApplied = new List<Registration> { m1, m2, m3, f1 };

        var retRepo = new Mock<IRetreatRepository>();
        var regRepo = new Mock<IRegistrationRepository>();

        retRepo.Setup(r => r.GetByIdAsync(retreatId, default)).ReturnsAsync(retreat);
        regRepo.Setup(r => r.CountByStatusesAndGenderAsync(retreatId, It.IsAny<RegistrationStatus[]>(), Gender.Male, default))
               .ReturnsAsync(0);
        regRepo.Setup(r => r.CountByStatusesAndGenderAsync(retreatId, It.IsAny<RegistrationStatus[]>(), Gender.Female, default))
               .ReturnsAsync(0);
        regRepo.Setup(r => r.ListAppliedByGenderAsync(retreatId, default))
               .ReturnsAsync(allApplied);

        var handler = new LotteryPreviewHandler(retRepo.Object, regRepo.Object);

        // Act
        var query = new LotteryPreviewQuery(retreatId, MinAge: 18, MaxAge: 25);
        var result = await handler.Handle(query, default);

        // Assert
        result.Male.Should().HaveCount(3);
        result.MalePriority.Should().ContainSingle(); // Só m2 (22 anos)
        result.MalePriority.Should().Contain(m2.Id);
        
        result.Female.Should().ContainSingle();
        result.FemalePriority.Should().ContainSingle(); // f1 (20 anos)
        result.FemalePriority.Should().Contain(f1.Id);
    }

    [Fact]
    public async Task Preview_com_prioridade_combinada_cidade_e_idade()
    {
        // Arrange
        var retreat = MakeRetreat(maleSlots: 3, femaleSlots: 1);
        var retreatId = retreat.Id;

        // Combinações:
        var m1 = MakeReg(retreatId, "Joao Silva", Gender.Male, "Recife", 2008);      // Prioritário (cidade + idade)
        var m2 = MakeReg(retreatId, "Pedro Costa", Gender.Male, "São Paulo", 2008);  // Prioritário (idade)
        var m3 = MakeReg(retreatId, "Carlos Dias", Gender.Male, "Recife", 1990);     // Prioritário (cidade)
        var m4 = MakeReg(retreatId, "Lucas Moura", Gender.Male, "Fortaleza", 1980);  // Regular
        var f1 = MakeReg(retreatId, "Maria Souza", Gender.Female, "Recife", 2010);   // Prioritária (cidade + idade)

        var allApplied = new List<Registration> { m1, m2, m3, m4, f1 };

        var retRepo = new Mock<IRetreatRepository>();
        var regRepo = new Mock<IRegistrationRepository>();

        retRepo.Setup(r => r.GetByIdAsync(retreatId, default)).ReturnsAsync(retreat);
        regRepo.Setup(r => r.CountByStatusesAndGenderAsync(retreatId, It.IsAny<RegistrationStatus[]>(), Gender.Male, default))
               .ReturnsAsync(0);
        regRepo.Setup(r => r.CountByStatusesAndGenderAsync(retreatId, It.IsAny<RegistrationStatus[]>(), Gender.Female, default))
               .ReturnsAsync(0);
        regRepo.Setup(r => r.ListAppliedByGenderAsync(retreatId, default))
               .ReturnsAsync(allApplied);

        var handler = new LotteryPreviewHandler(retRepo.Object, regRepo.Object);

        // Act
        var query = new LotteryPreviewQuery(
            retreatId, 
            PriorityCities: new List<string> { "Recife" }, 
            MinAge: 18, 
            MaxAge: 25);
        var result = await handler.Handle(query, default);

        // Assert
        result.Male.Should().HaveCount(3);
        result.MalePriority.Should().HaveCount(3); // m1, m2, m3 (todos prioritários)
        result.MalePriority.Should().Contain(new[] { m1.Id, m2.Id, m3.Id });
        
        result.Female.Should().ContainSingle();
        result.FemalePriority.Should().ContainSingle();
        result.FemalePriority.Should().Contain(f1.Id);
    }

    [Fact]
    public async Task Preview_falha_se_contemplacao_fechada()
    {
        // Arrange
        var retreat = MakeRetreat();
        retreat.CloseContemplation();

        var retRepo = new Mock<IRetreatRepository>();
        var regRepo = new Mock<IRegistrationRepository>();

        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, default)).ReturnsAsync(retreat);

        var handler = new LotteryPreviewHandler(retRepo.Object, regRepo.Object);

        // Act & Assert
        await FluentActions.Invoking(() => 
            handler.Handle(new LotteryPreviewQuery(retreat.Id), default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*closed*");
    }
}
