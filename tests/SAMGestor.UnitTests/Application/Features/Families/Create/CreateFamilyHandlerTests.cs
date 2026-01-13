using FluentAssertions;
using Moq;
using SAMGestor.Application.Features.Families.Create;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.UnitTests.Application.Features.Families.Create;

public class CreateFamilyHandlerTests
{
    private static Retreat OpenRetreat()
        => new Retreat(
            new FullName("Retiro X"), "ED1", "Tema",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(12)),
            50, 50,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            new Money(0,"BRL"), new Money(0,"BRL"),
            new Percentage(50), new Percentage(50));

    private static Registration NewReg(Guid retreatId, string name, Gender g,
        RegistrationStatus st = RegistrationStatus.Confirmed, string city = "SP", bool enabled = true)
    {
        var reg = new Registration(
            new FullName(name),
            new CPF("52998224725"),
            new EmailAddress($"{Guid.NewGuid()}@mail.com"),
            "11999999999",
            new DateOnly(1990,1,1),
            g,
            city,
            st,
            retreatId);

        if (!enabled) reg.Disable();
        return reg;
    }

    private static FamilyMember Link(Guid retreatId, Guid familyId, Guid regId, int pos, bool isPadrinho = false, bool isMadrinha = false)
        => new FamilyMember(retreatId, familyId, regId, pos, isPadrinho, isMadrinha);

    private static Family NewFamily(Guid retreatId, string name = "Família 1", string colorName = "Azul")
    {
        var color = FamilyColor.FromName(colorName);
        return new Family(new FamilyName(name), retreatId, 4, color);
    }

    private static CreateFamilyHandler BuildHandler(
        Retreat retreat,
        out Mock<IRetreatRepository> retRepo,
        out Mock<IFamilyRepository> familyRepo,
        out Mock<IFamilyMemberRepository> fmRepo,
        out Mock<IRegistrationRepository> regRepo,
        out Mock<IUnitOfWork> uow)
    {
        retRepo   = new Mock<IRetreatRepository>();
        familyRepo= new Mock<IFamilyRepository>();
        fmRepo    = new Mock<IFamilyMemberRepository>();
        regRepo   = new Mock<IRegistrationRepository>();
        uow       = new Mock<IUnitOfWork>();

        retRepo.Setup(r => r.GetByIdAsync(retreat.Id, It.IsAny<CancellationToken>()))
               .ReturnsAsync(retreat);
        retRepo.Setup(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()))
               .Returns(Task.CompletedTask);

        familyRepo.Setup(f => f.AddAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);

        fmRepo.Setup(f => f.AddRangeAsync(It.IsAny<IEnumerable<FamilyMember>>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);

        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);

        return new CreateFamilyHandler(retRepo.Object, familyRepo.Object, fmRepo.Object, regRepo.Object, uow.Object);
    }
    

    [Fact]
    public async Task Falha_quando_lock_global()
    {
        var retreat = OpenRetreat();
        retreat.LockFamilies();

        var handler = BuildHandler(retreat, out _, out _, out _, out _, out _);

        var cmd = new CreateFamilyCommand(
            RetreatId: retreat.Id,
            Name: null,
            ColorName: "Azul",
            Capacity: 4,
            MemberIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },
            PadrinhoIds: null,
            MadrinhaIds: null,
            IgnoreWarnings: true);

        await FluentActions.Invoking(() => handler.Handle(cmd, default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*bloqueadas*");
    }

    [Fact]
    public async Task Falha_quando_qtd_membros_menor_que_4()
    {
        var retreat = OpenRetreat();
        var handler = BuildHandler(retreat, out _, out _, out _, out _, out _);

        var cmd = new CreateFamilyCommand(
            retreat.Id,
            null,
            "Azul",
            Capacity: 3,
            MemberIds: new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },
            null,
            null,
            true);

        await FluentActions.Invoking(() => handler.Handle(cmd, default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*no mínimo 4*");
    }

    [Fact]
    public async Task Falha_quando_ids_duplicados()
    {
        var retreat = OpenRetreat();
        var handler = BuildHandler(retreat, out _, out _, out _, out _, out _);

        var g = Guid.NewGuid();
        var cmd = new CreateFamilyCommand(
            retreat.Id,
            null,
            "Verde",
            Capacity: 4,
            new List<Guid> { g, g, g, g },
            null,
            null,
            true);

        await FluentActions.Invoking(() => handler.Handle(cmd, default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*duplicados*");
    }

    [Fact]
    public async Task Falha_quando_registration_nao_existe()
    {
        var retreat = OpenRetreat();
        var handler = BuildHandler(retreat, out _, out _, out _, out var regRepo, out _);

        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() }.ToList();
        regRepo.Setup(r => r.GetMapByIdsAsync(ids, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration>()); 

        var cmd = new CreateFamilyCommand(retreat.Id, null, "Vermelho", 4, ids, null, null, true);

        await FluentActions.Invoking(() => handler.Handle(cmd, default))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Registration*");
    }

    [Fact]
    public async Task Falha_quando_membro_de_outro_retiro()
    {
        var retreat = OpenRetreat();
        var otherRetreat = OpenRetreat();

        var r1 = NewReg(otherRetreat.Id, "Joao Silva", Gender.Male);
        var r2 = NewReg(retreat.Id, "Pedro Souza", Gender.Male);
        var r3 = NewReg(retreat.Id, "Ana Lima", Gender.Female);
        var r4 = NewReg(retreat.Id, "Bea Rocha", Gender.Female);

        var ids = new List<Guid> { r1.Id, r2.Id, r3.Id, r4.Id };

        var handler = BuildHandler(retreat, out _, out _, out _, out var regRepo, out _);
        regRepo.Setup(r => r.GetMapByIdsAsync(ids, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> {
                   [r1.Id]=r1, [r2.Id]=r2, [r3.Id]=r3, [r4.Id]=r4
               });

        var cmd = new CreateFamilyCommand(retreat.Id, null, "Amarelo", 4, ids, null, null, true);

        await FluentActions.Invoking(() => handler.Handle(cmd, default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*mesmo retiro*");
    }

    [Fact]
    public async Task Falha_quando_nao_habilitado()
    {
        var retreat = OpenRetreat();

        var r1 = NewReg(retreat.Id, "Joao Silva", Gender.Male, enabled:false);
        var r2 = NewReg(retreat.Id, "Pedro Souza", Gender.Male);
        var r3 = NewReg(retreat.Id, "Ana Lima", Gender.Female);
        var r4 = NewReg(retreat.Id, "Bea Rocha", Gender.Female);

        var ids = new List<Guid> { r1.Id, r2.Id, r3.Id, r4.Id };

        var handler = BuildHandler(retreat, out _, out _, out _, out var regRepo, out _);
        regRepo.Setup(r => r.GetMapByIdsAsync(ids, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> {
                   [r1.Id]=r1, [r2.Id]=r2, [r3.Id]=r3, [r4.Id]=r4
               });

        var cmd = new CreateFamilyCommand(retreat.Id, null, "Roxo", Capacity: 4, MemberIds: ids, PadrinhoIds: null, null, true);

        await FluentActions.Invoking(() => handler.Handle(cmd, default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*desabilitado*");
    }

    [Fact]
    public async Task Falha_quando_status_nao_confirmado()
    {
        var retreat = OpenRetreat();

        var r1 = NewReg(retreat.Id, "Joao Silva", Gender.Male, RegistrationStatus.NotSelected);
        var r2 = NewReg(retreat.Id, "Pedro Souza", Gender.Male);
        var r3 = NewReg(retreat.Id, "Ana Lima", Gender.Female);
        var r4 = NewReg(retreat.Id, "Bea Rocha", Gender.Female);

        var ids = new List<Guid> { r1.Id, r2.Id, r3.Id, r4.Id };

        var handler = BuildHandler(retreat, out _, out _, out _, out var regRepo, out _);
        regRepo.Setup(r => r.GetMapByIdsAsync(ids, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> {
                   [r1.Id]=r1, [r2.Id]=r2, [r3.Id]=r3, [r4.Id]=r4
               });

        var cmd = new CreateFamilyCommand(retreat.Id, null, "Laranja", Capacity: 4, MemberIds: ids, PadrinhoIds: null, null, true);

        await FluentActions.Invoking(() => handler.Handle(cmd, default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*não está confirmado/pago*");
    }

    [Fact]
    public async Task Falha_quando_alguem_ja_alocado()
    {
        var retreat = OpenRetreat();

        var r1 = NewReg(retreat.Id, "Joao Silva", Gender.Male);
        var r2 = NewReg(retreat.Id, "Pedro Souza", Gender.Male);
        var r3 = NewReg(retreat.Id, "Ana Lima", Gender.Female);
        var r4 = NewReg(retreat.Id, "Bea Rocha", Gender.Female);

        var ids = new List<Guid> { r1.Id, r2.Id, r3.Id, r4.Id };

        var handler = BuildHandler(retreat, out _, out _, out var fmRepo, out var regRepo, out _);
        regRepo.Setup(r => r.GetMapByIdsAsync(ids, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> {
                   [r1.Id]=r1, [r2.Id]=r2, [r3.Id]=r3, [r4.Id]=r4
               });

        fmRepo.Setup(f => f.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<FamilyMember> { Link(retreat.Id, Guid.NewGuid(), r2.Id, 0) });

        var cmd = new CreateFamilyCommand(retreat.Id, null, "Rosa", Capacity:4 , ids, null, null, true);

        await FluentActions.Invoking(() => handler.Handle(cmd, default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*já estão alocados*");
    }
    

    [Fact]
    public async Task Falha_quando_cor_invalida()
    {
        var retreat = OpenRetreat();
        var r1 = NewReg(retreat.Id, "Joao Silva", Gender.Male);
        var r2 = NewReg(retreat.Id, "Pedro Souza", Gender.Male);
        var r3 = NewReg(retreat.Id, "Ana Lima", Gender.Female);
        var r4 = NewReg(retreat.Id, "Bea Rocha", Gender.Female);

        var ids = new List<Guid> { r1.Id, r2.Id, r3.Id, r4.Id };

        var handler = BuildHandler(retreat, out _, out var familyRepo, out var fmRepo, out var regRepo, out _);
        regRepo.Setup(r => r.GetMapByIdsAsync(ids, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> {
                   [r1.Id]=r1, [r2.Id]=r2, [r3.Id]=r3, [r4.Id]=r4
               });

        fmRepo.Setup(f => f.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<FamilyMember>());

        familyRepo.Setup(f => f.ColorExistsInRetreatAsync(retreat.Id, It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(false);

        var cmd = new CreateFamilyCommand(retreat.Id, null, "CorInexistente", 4, ids, null, null, true);

        await FluentActions.Invoking(() => handler.Handle(cmd, default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*não está disponível*");
    }

    [Fact]
    public async Task Falha_quando_cor_ja_em_uso()
    {
        var retreat = OpenRetreat();
        var r1 = NewReg(retreat.Id, "Joao Silva", Gender.Male);
        var r2 = NewReg(retreat.Id, "Pedro Souza", Gender.Male);
        var r3 = NewReg(retreat.Id, "Ana Lima", Gender.Female);
        var r4 = NewReg(retreat.Id, "Bea Rocha", Gender.Female);

        var ids = new List<Guid> { r1.Id, r2.Id, r3.Id, r4.Id };

        var handler = BuildHandler(retreat, out _, out var familyRepo, out var fmRepo, out var regRepo, out _);
        regRepo.Setup(r => r.GetMapByIdsAsync(ids, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> {
                   [r1.Id]=r1, [r2.Id]=r2, [r3.Id]=r3, [r4.Id]=r4
               });

        fmRepo.Setup(f => f.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<FamilyMember>());
        
        familyRepo.Setup(f => f.ColorExistsInRetreatAsync(retreat.Id, "Azul", null, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(true);

        var cmd = new CreateFamilyCommand(retreat.Id, null, "Azul", 4, ids, null, null, true);

        await FluentActions.Invoking(() => handler.Handle(cmd, default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*já está sendo utilizada*");
    }
    

    [Fact]
    public async Task Falha_quando_padrinho_nao_e_membro_da_familia()
    {
        var retreat = OpenRetreat();
        var r1 = NewReg(retreat.Id, "Joao Silva", Gender.Male);
        var r2 = NewReg(retreat.Id, "Pedro Souza", Gender.Male);
        var r3 = NewReg(retreat.Id, "Ana Lima", Gender.Female);
        var r4 = NewReg(retreat.Id, "Bea Rocha", Gender.Female);

        var ids = new List<Guid> { r1.Id, r2.Id, r3.Id, r4.Id };
        var outroId = Guid.NewGuid();

        var handler = BuildHandler(retreat, out _, out var familyRepo, out var fmRepo, out var regRepo, out _);
        regRepo.Setup(r => r.GetMapByIdsAsync(ids, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> {
                   [r1.Id]=r1, [r2.Id]=r2, [r3.Id]=r3, [r4.Id]=r4
               });

        fmRepo.Setup(f => f.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<FamilyMember>());

        familyRepo.Setup(f => f.ColorExistsInRetreatAsync(retreat.Id, It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(false);

        var cmd = new CreateFamilyCommand(
            retreat.Id,
            null,
            "Ciano",
            Capacity: 4,
            ids,
            PadrinhoIds: new List<Guid> { outroId },
            MadrinhaIds: null,
            IgnoreWarnings: true);

        await FluentActions.Invoking(() => handler.Handle(cmd, default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*devem estar na lista de membros*");
    }

    [Fact]
    public async Task Falha_quando_padrinho_nao_e_masculino()
    {
        var retreat = OpenRetreat();
        var r1 = NewReg(retreat.Id, "Joao Silva", Gender.Male);
        var r2 = NewReg(retreat.Id, "Pedro Souza", Gender.Male);
        var r3 = NewReg(retreat.Id, "Ana Lima", Gender.Female);
        var r4 = NewReg(retreat.Id, "Bea Rocha", Gender.Female);

        var ids = new List<Guid> { r1.Id, r2.Id, r3.Id, r4.Id };

        var handler = BuildHandler(retreat, out _, out var familyRepo, out var fmRepo, out var regRepo, out _);
        regRepo.Setup(r => r.GetMapByIdsAsync(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, Registration> {
                [r1.Id]=r1, [r2.Id]=r2, [r3.Id]=r3, [r4.Id]=r4
            });

        fmRepo.Setup(f => f.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FamilyMember>());

        familyRepo.Setup(f => f.ColorExistsInRetreatAsync(retreat.Id, It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var cmd = new CreateFamilyCommand(
            retreat.Id,
            null,
            "Verde",
            Capacity: 4,
            ids,
            PadrinhoIds: new List<Guid> { r3.Id }, 
            MadrinhaIds: null,
            IgnoreWarnings: true);

        await FluentActions.Invoking(() => handler.Handle(cmd, default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*gênero masculino*");
    }

    [Fact]
    public async Task Falha_quando_madrinha_nao_e_feminina()
    {
        var retreat = OpenRetreat();
        var r1 = NewReg(retreat.Id, "Joao Silva", Gender.Male);
        var r2 = NewReg(retreat.Id, "Pedro Souza", Gender.Male);
        var r3 = NewReg(retreat.Id, "Ana Lima", Gender.Female);
        var r4 = NewReg(retreat.Id, "Bea Rocha", Gender.Female);

        var ids = new List<Guid> { r1.Id, r2.Id, r3.Id, r4.Id };

        var handler = BuildHandler(retreat, out _, out var familyRepo, out var fmRepo, out var regRepo, out _);
        regRepo.Setup(r => r.GetMapByIdsAsync(ids, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> {
                   [r1.Id]=r1, [r2.Id]=r2, [r3.Id]=r3, [r4.Id]=r4
               });

        fmRepo.Setup(f => f.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<FamilyMember>());

        familyRepo.Setup(f => f.ColorExistsInRetreatAsync(retreat.Id, It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(false);

        var cmd = new CreateFamilyCommand(
            retreat.Id,
            null,
            "Turquesa",
            Capacity: 4,
            ids,
            PadrinhoIds: null,
            MadrinhaIds: new List<Guid> { r1.Id }, 
            IgnoreWarnings: true);

        await FluentActions.Invoking(() => handler.Handle(cmd, default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*gênero feminino*");
    }

    [Fact]
    public async Task Falha_quando_mais_de_2_padrinhos()
    {
        var retreat = OpenRetreat();
        var r1 = NewReg(retreat.Id, "Joao Silva", Gender.Male);
        var r2 = NewReg(retreat.Id, "Pedro Souza", Gender.Male);
        var r3 = NewReg(retreat.Id, "Carlos Lima", Gender.Male);
        var r4 = NewReg(retreat.Id, "Ana Rocha", Gender.Female);

        var ids = new List<Guid> { r1.Id, r2.Id, r3.Id, r4.Id };

        var handler = BuildHandler(retreat, out _, out var familyRepo, out var fmRepo, out var regRepo, out _);
        regRepo.Setup(r => r.GetMapByIdsAsync(ids, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, Registration> {
                [r1.Id]=r1, [r2.Id]=r2, [r3.Id]=r3, [r4.Id]=r4
            });

        fmRepo.Setup(f => f.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FamilyMember>());

        familyRepo.Setup(f => f.ColorExistsInRetreatAsync(retreat.Id, It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var cmd = new CreateFamilyCommand(
            retreat.Id,
            null,
            "Marrom",
            Capacity: 4,
            ids,
            PadrinhoIds: new List<Guid> { r1.Id, r2.Id, r3.Id }, 
            MadrinhaIds: null,
            IgnoreWarnings: true);

        await FluentActions.Invoking(() => handler.Handle(cmd, default))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*mais de 2 padrinhos*");
    }
    

    [Fact]
    public async Task Warning_same_city_retorna_created_false_quando_ignore_false()
    {
        var retreat = OpenRetreat();

        var r1 = NewReg(retreat.Id, "Joao Silva", Gender.Male, city:"Recife");
        var r2 = NewReg(retreat.Id, "Pedro Souza", Gender.Male, city:"Recife");
        var r3 = NewReg(retreat.Id, "Ana Lima", Gender.Female, city:"Olinda");
        var r4 = NewReg(retreat.Id, "Bea Rocha", Gender.Female, city:"Caruaru");

        var ids = new List<Guid> { r1.Id, r2.Id, r3.Id, r4.Id };

        var handler = BuildHandler(retreat, out _, out var familyRepo, out var fmRepo, out var regRepo, out var uow);
        regRepo.Setup(r => r.GetMapByIdsAsync(ids, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> {
                   [r1.Id]=r1, [r2.Id]=r2, [r3.Id]=r3, [r4.Id]=r4
               });
        
        fmRepo.Setup(f => f.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<FamilyMember>());
        
        familyRepo.Setup(f => f.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<Family>());

        familyRepo.Setup(f => f.ColorExistsInRetreatAsync(retreat.Id, It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(false);

        var cmd = new CreateFamilyCommand(
            retreat.Id,
            null,
            "Azul",
            Capacity: 4,
            ids,
            PadrinhoIds: null,
            MadrinhaIds: null,
            IgnoreWarnings: false);
        var res = await handler.Handle(cmd, default);

        res.Created.Should().BeFalse();
        res.Warnings.Should().NotBeEmpty();
        res.Warnings.Should().Contain(w => w.Code == "SAME_CITY");
        
        familyRepo.Verify(f => f.AddAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>()), Times.Never);
        fmRepo.Verify(f => f.AddRangeAsync(It.IsAny<IEnumerable<FamilyMember>>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Warning_missing_godparents_quando_nao_informado()
    {
        var retreat = OpenRetreat();

        var r1 = NewReg(retreat.Id, "Joao Silva", Gender.Male);
        var r2 = NewReg(retreat.Id, "Pedro Souza", Gender.Male);
        var r3 = NewReg(retreat.Id, "Ana Lima", Gender.Female);
        var r4 = NewReg(retreat.Id, "Bea Rocha", Gender.Female);

        var ids = new List<Guid> { r1.Id, r2.Id, r3.Id, r4.Id };

        var handler = BuildHandler(retreat, out _, out var familyRepo, out var fmRepo, out var regRepo, out _);
        regRepo.Setup(r => r.GetMapByIdsAsync(ids, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> {
                   [r1.Id]=r1, [r2.Id]=r2, [r3.Id]=r3, [r4.Id]=r4
               });

        fmRepo.Setup(f => f.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<FamilyMember>());

        familyRepo.Setup(f => f.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<Family>());

        familyRepo.Setup(f => f.ColorExistsInRetreatAsync(retreat.Id, It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(false);

        var cmd = new CreateFamilyCommand(
            retreat.Id,
            null,
            "Azul",
            Capacity: 4,
            ids,
            PadrinhoIds: null,
            MadrinhaIds: null,
            IgnoreWarnings: false);

        var res = await handler.Handle(cmd, default);

        res.Created.Should().BeFalse();
        res.Warnings.Should().Contain(w => w.Code == "MISSING_GODPARENTS");
    }
    

    [Fact]
    public async Task Sucesso_com_warning_ignore_true_persiste_e_incrementa_versao()
    {
        var retreat = OpenRetreat();

        var r1 = NewReg(retreat.Id, "Joao Silva", Gender.Male, city:"Recife");
        var r2 = NewReg(retreat.Id, "Pedro Souza", Gender.Male, city:"Recife"); // provoca SAME_CITY
        var r3 = NewReg(retreat.Id, "Ana Lima", Gender.Female);
        var r4 = NewReg(retreat.Id, "Bea Rocha", Gender.Female);

        var ids = new List<Guid> { r1.Id, r2.Id, r3.Id, r4.Id };

        var handler = BuildHandler(retreat, out var retRepo, out var familyRepo, out var fmRepo, out var regRepo, out var uow);
        regRepo.Setup(r => r.GetMapByIdsAsync(ids, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> {
                   [r1.Id]=r1, [r2.Id]=r2, [r3.Id]=r3, [r4.Id]=r4
               });

        fmRepo.Setup(f => f.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<FamilyMember>());
        
        familyRepo.Setup(f => f.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<Family> { NewFamily(retreat.Id, "Família 1", "Azul") });

        familyRepo.Setup(f => f.ColorExistsInRetreatAsync(retreat.Id, It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(false);

        Family? created = null;
        familyRepo.Setup(f => f.AddAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>()))
                  .Callback<Family, CancellationToken>((fam, _) => created = fam)
                  .Returns(Task.CompletedTask);

        var cmd = new CreateFamilyCommand(retreat.Id, Name: null, ColorName: "Verde", Capacity: 4, MemberIds: ids, PadrinhoIds: new List<Guid> { r1.Id, r2.Id }, MadrinhaIds: new List<Guid> { r3.Id, r4.Id }, IgnoreWarnings: true);
        var res = await handler.Handle(cmd, default);

        res.Created.Should().BeTrue();
        res.FamilyId.Should().NotBeNull();
        created.Should().NotBeNull();
        ((string)created!.Name).Should().Be("Família 2");
        created.Color.Name.Should().Be("Verde");

        familyRepo.Verify(f => f.AddAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>()), Times.Once);
        
        // Verifica que criou 4 membros com padrinhos/madrinhas marcados
        fmRepo.Verify(f => f.AddRangeAsync(
            It.Is<IEnumerable<FamilyMember>>(x => 
                x.Count() == 4 && 
                x.Count(m => m.IsPadrinho) == 2 &&
                x.Count(m => m.IsMadrinha) == 2), 
            It.IsAny<CancellationToken>()), Times.Once);
        
        retRepo.Verify(r => r.UpdateAsync(retreat, It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        res.Version.Should().Be(retreat.FamiliesVersion);
    }

    [Fact]
    public async Task Sucesso_quando_nome_fornecido_sem_colisao()
    {
        var retreat = OpenRetreat();

        var r1 = NewReg(retreat.Id, "Joao Silva", Gender.Male);
        var r2 = NewReg(retreat.Id, "Pedro Souza", Gender.Male);
        var r3 = NewReg(retreat.Id, "Ana Lima", Gender.Female);
        var r4 = NewReg(retreat.Id, "Bea Rocha", Gender.Female);

        var ids = new List<Guid> { r1.Id, r2.Id, r3.Id, r4.Id };

        var handler = BuildHandler(retreat, out _, out var familyRepo, out var fmRepo, out var regRepo, out var uow);
        regRepo.Setup(r => r.GetMapByIdsAsync(ids, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> {
                   [r1.Id]=r1, [r2.Id]=r2, [r3.Id]=r3, [r4.Id]=r4
               });

        fmRepo.Setup(f => f.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<FamilyMember>());
        
        familyRepo.Setup(f => f.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<Family>());

        familyRepo.Setup(f => f.ColorExistsInRetreatAsync(retreat.Id, It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(false);

        Family? created = null;
        familyRepo.Setup(f => f.AddAsync(It.IsAny<Family>(), It.IsAny<CancellationToken>()))
                  .Callback<Family, CancellationToken>((fam, _) => created = fam)
                  .Returns(Task.CompletedTask);

        var cmd = new CreateFamilyCommand(retreat.Id, "Ministério A", "Roxo", 4, ids, null, null, true);
        var res = await handler.Handle(cmd, default);

        res.Created.Should().BeTrue();
        ((string)created!.Name).Should().Be("Ministério A");
        created.Color.Name.Should().Be("Roxo");
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Sucesso_com_apenas_1_padrinho_e_1_madrinha_gera_warning_mas_persiste()
    {
        var retreat = OpenRetreat();

        var r1 = NewReg(retreat.Id, "Joao Silva", Gender.Male);
        var r2 = NewReg(retreat.Id, "Pedro Souza", Gender.Male);
        var r3 = NewReg(retreat.Id, "Ana Lima", Gender.Female);
        var r4 = NewReg(retreat.Id, "Bea Rocha", Gender.Female);

        var ids = new List<Guid> { r1.Id, r2.Id, r3.Id, r4.Id };

        var handler = BuildHandler(retreat, out _, out var familyRepo, out var fmRepo, out var regRepo, out _);
        regRepo.Setup(r => r.GetMapByIdsAsync(ids, It.IsAny<CancellationToken>()))
               .ReturnsAsync(new Dictionary<Guid, Registration> {
                   [r1.Id]=r1, [r2.Id]=r2, [r3.Id]=r3, [r4.Id]=r4
               });

        fmRepo.Setup(f => f.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<FamilyMember>());

        familyRepo.Setup(f => f.ListByRetreatAsync(retreat.Id, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(new List<Family>());

        familyRepo.Setup(f => f.ColorExistsInRetreatAsync(retreat.Id, It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(false);

        var cmd = new CreateFamilyCommand(
            retreat.Id,
            null,
            "Amarelo",
            Capacity: 4,
            ids,
            PadrinhoIds: new List<Guid> { r1.Id }, // só 1
            MadrinhaIds: new List<Guid> { r3.Id }, // só 1
            IgnoreWarnings: true);

        var res = await handler.Handle(cmd, default);

        res.Created.Should().BeTrue();
        res.Warnings.Should().Contain(w => w.Code == "MISSING_GODPARENTS");
    }
}
